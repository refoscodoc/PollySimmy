using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Fallback;
using Polly.Retry;
using PollySimmy.Commands;
using PollySimmy.Queries;

namespace PollySimmy.Controllers;

[ApiController]
[Route("[controller]")]
public class HorseController : ControllerBase
{
    private readonly AsyncRetryPolicy _fallbackPolicy;
    private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreaker;
    private readonly AsyncInjectOutcomePolicy<HttpResponseMessage> _faultPolicy;
    private readonly IMediator _mediator;
    private readonly HttpClient _client;

    public HorseController(IMediator mediator, IHttpClientFactory clientFactory, AsyncInjectOutcomePolicy<HttpResponseMessage> faultPolicy)
    {
        _fallbackPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(
            attempt => TimeSpan.FromMilliseconds(200), (exception, waitDuration) => Console.WriteLine($"Something went wrong: {exception}. Waiting {waitDuration}ms."));
        
        _circuitBreaker = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .AdvancedCircuitBreakerAsync(0.25, TimeSpan.FromSeconds(60), 7, TimeSpan.FromSeconds(30));
        
        _faultPolicy = faultPolicy;
        
        _mediator = mediator;
        _client = clientFactory.CreateClient();
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
            var breweries = await response.Content.ReadAsStringAsync();
            var breweriesList = JsonConvert.DeserializeObject<dynamic>(breweries);
            return Ok(breweriesList.ToString());
        }

        return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
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
        await _fallbackPolicy.ExecuteAsync(async () => response = await _client.GetStringAsync("http://localhost:5111/RandomJoke"));

        var joke = JsonConvert.DeserializeObject<dynamic>(response);

        var newHoreResult = newHorse with {Id = Guid.NewGuid(), HorseJoke = joke?.setup ?? joke?.joke};
        
        var result = await _mediator.Send(newHoreResult);
        return CreatedAtAction("Get", new { id = result.Id }, newHoreResult);
    }
}