using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Fallback;
using Polly.Retry;
using Polly.Wrap;
using PollySimmy.Commands;
using PollySimmy.Models;
using PollySimmy.Queries;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PollySimmy.Controllers;

[ApiController]
[Route("[controller]")]
public class HorseController : ControllerBase
{
    private readonly AsyncRetryPolicy _retryForeverPolicy;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly AsyncInjectOutcomePolicy<HttpResponseMessage> _faultPolicy;
    private readonly AsyncInjectOutcomePolicy<HttpResponseMessage> _wrappedPolicy;
    private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreaker;
    // private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerAdvanced;
    private readonly AsyncFallbackPolicy<HttpResponseMessage> _fallbackPolicy;
    private readonly IMediator _mediator;
    private readonly HttpClient _client;
    private readonly IHttpClientFactory _baconClient;

    public HorseController(IMediator mediator, 
        IHttpClientFactory clientFactory, 
        AsyncInjectOutcomePolicy<HttpResponseMessage> faultPolicy, 
        IHttpClientFactory baconClient, 
        AsyncInjectOutcomePolicy<HttpResponseMessage> wrappedPolicy)
    {
        _retryPolicy = Policy.HandleResult<HttpResponseMessage>(
                msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(10, attempt =>
            {
                Console.WriteLine(($"Retrying due to an error. Attempt {attempt}"));
                return TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(1000);
            });
        
        _retryForeverPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(
            attempt => TimeSpan.FromMilliseconds(200), (exception, waitDuration) => Console.WriteLine($"Something went wrong: {exception}. Waiting {waitDuration}ms."));

        _circuitBreaker = Policy.HandleResult<HttpResponseMessage>(
                msg => ((int)msg.StatusCode == 503) || !msg.IsSuccessStatusCode)
            .CircuitBreakerAsync(3, TimeSpan.FromSeconds(15),
                onBreak: (_, duration) => Console.WriteLine($"Circuit tripped. Circuit is open and requests won't be allowed through for duration={duration}"),
                onReset: () => Console.WriteLine("Circuit closed. Requests are now allowed through"),
                onHalfOpen: () => Console.WriteLine("Circuit is now half-opened and will test the service with the next request"));
        
        // _circuitBreakerAdvanced = Policy
        //     .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        //     .AdvancedCircuitBreakerAsync(3, TimeSpan.FromSeconds(15), 7, TimeSpan.FromSeconds(30));

        _fallbackPolicy = Policy
            .HandleResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
            .FallbackAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                d =>
                {
                    Console.WriteLine("Unavailable. Retrying again later.");
                    return Task.CompletedTask;
                });

        _faultPolicy = faultPolicy;
        _wrappedPolicy = wrappedPolicy;
        
        _mediator = mediator;
        _client = clientFactory.CreateClient();
        _baconClient = baconClient;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out _))
        {
            var resultUid = await _mediator.Send(new GetHorseByNameQuery(id));
            
            if(!ModelState.IsValid) throw new ArgumentNullException(nameof(id));
            
            return Ok(resultUid);
        }

        var guid = Guid.Parse(id);
        
        var result = await _mediator.Send(new GetHorseByIdQuery(guid));
        
        return result != null ? (IActionResult) Ok(result) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllHorsesQuery());
        return Ok(result);
    }

    [HttpGet]
    [Route("Polly")]
    public async Task<IActionResult> GetAllPolly()
    {
        var fault = new SocketException(errorCode: 10013);
        var chaosPolicy = MonkeyPolicy.InjectExceptionAsync(with =>
            with.Fault(fault)
                .InjectionRate(0.4)
                .Enabled()
        );
        var chaosLatencyPolicy = MonkeyPolicy.InjectLatencyAsync(with =>
            with.Latency(TimeSpan.FromSeconds(5))
                .InjectionRate(0.4)
                .Enabled()
        );
        var policyWrap = Policy
            .WrapAsync(chaosPolicy, chaosLatencyPolicy);
        
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        var response = await policyWrap.ExecuteAsync(() => _client.GetAsync("https://api.openbrewerydb.org/breweries/"));
        // var response = await _faultPolicy.ExecuteAsync(() => _client.GetAsync("https://api.openbrewerydb.org/breweries/"));
        
        if (response.IsSuccessStatusCode)
        {
            var horsies = await response.Content.ReadAsStringAsync();
            var horsiesList = JsonConvert.DeserializeObject<dynamic>(horsies);
            return Ok(horsiesList.ToString());
        }

        return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
    }

    [HttpGet]
    [Route("CircuitBreaker")]
    public async Task<IActionResult> GetAllCircuitBreaker()
    {
        var httpClient = _baconClient.CreateClient("BaconService");
        
        AsyncPolicyWrap<HttpResponseMessage> pollyCircuitBreakerWrap = Policy.WrapAsync(_fallbackPolicy, _retryPolicy, _circuitBreaker);

        #region SimmyPolicies

        var result = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        AsyncInjectOutcomePolicy<HttpResponseMessage> chaosPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(
            with =>             
                with.Result(result)
                .InjectionRate(1)
                .Enabled()
        );
        
        var chaosLatencyPolicy = MonkeyPolicy.InjectLatencyAsync(with =>
            with.Latency(TimeSpan.FromSeconds(2))
                .InjectionRate(1)
                .Enabled()
        );

        #endregion

        Console.WriteLine("Fetching data..");
        var resulting = _retryPolicy.WrapAsync(_circuitBreaker).ExecuteAsync(async () => chaosPolicy.ExecuteAsync(async () =>
        {
            if (_circuitBreaker.CircuitState == CircuitState.Open)
            {
                throw new Exception("The service is currently unavailable");
            }
            return await httpClient.GetAsync("?type=meat-and-filler/");
        }).Result);
                
        return Ok(resulting.Result.StatusCode);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddHorse([FromBody] AddHorseCommand? newHorse)
    {
        if (newHorse is null)
            return BadRequest(new ArgumentNullException());
        
        if (!ModelState.IsValid)
            return BadRequest(new ArgumentNullException());

        string response = "";
        await _retryForeverPolicy.ExecuteAsync(async () => response = await _client.GetStringAsync("http://localhost:5111/RandomJoke"));

        var joke = JsonConvert.DeserializeObject<dynamic>(response);

        var newHoreResult = newHorse with {Id = Guid.NewGuid(), HorseJoke = joke?.setup ?? joke?.joke};
        
        var result = await _mediator.Send(newHoreResult);
        return CreatedAtAction("Get", new { id = result.Id }, newHoreResult);
    }
}