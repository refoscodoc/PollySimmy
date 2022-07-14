using MediatR;
using Microsoft.AspNetCore.Mvc;
using PollySimmy.Commands;
using PollySimmy.Queries;

namespace PollySimmy.Controllers;

[ApiController]
[Route("[controller]")]
public class StableController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly HttpClient _client;

    public StableController(IMediator mediator, IHttpClientFactory client)
    {
        _mediator = mediator;
        _client = client.CreateClient();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        if (!Guid.TryParse(id, out _))
        {
            var resultUid = await _mediator.Send(new GetStableByNameQuery(id));
            
            if(!ModelState.IsValid) throw new ArgumentNullException(nameof(id));
            
            return Ok(resultUid);
        }

        var guid = Guid.Parse(id);
        
        var result = await _mediator.Send(new GetStableByIdQuery(guid));
        
        return result != null ? (IActionResult) Ok(result) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllStablesQuery());
        return Ok(result);
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddStable([FromBody] AddStableCommand? newStable)
    {
        if (newStable is null)
            return BadRequest(new ArgumentNullException());
        
        if (!ModelState.IsValid)
            return BadRequest(new ArgumentNullException());
        
        var response = _client.GetStringAsync("http://localhost:5123/RandomWord");
        var motto = await response;
        
        var result = await _mediator.Send(newStable with {Id = Guid.NewGuid(), Motto = motto});
        return CreatedAtAction("Get", new { id = result.Id}, result);
    }
}