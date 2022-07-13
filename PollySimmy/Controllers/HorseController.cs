using MediatR;
using Microsoft.AspNetCore.Mvc;
using PollySimmy.Commands;
using PollySimmy.Queries;

namespace PollySimmy.Controllers;

[ApiController]
[Route("[controller]")]
public class HorseController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly HttpClient _client;

    public HorseController(IMediator mediator, IHttpClientFactory clientFactory)
    {
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
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddHorse([FromBody] AddHorseCommand? newHorse)
    {
        if (newHorse is null)
            return BadRequest(new ArgumentNullException());
        
        if (!ModelState.IsValid)
            return BadRequest(new ArgumentNullException());

        var response = _client.GetStringAsync("https://localhost:7118/RandomJoke");
        var joke = await response;
        
        var result = await _mediator.Send(newHorse with {Id = Guid.NewGuid(), HorseJoke = joke});
        return CreatedAtAction("Get", new { id = result.Id}, result);
    }
}