using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace RandomJoke.Controllers;

[ApiController]
[Route("[controller]")]
public class RandomJokeController : ControllerBase
{
    private readonly HttpClient _client;

    public RandomJokeController(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient();
    }

    [HttpGet]
    public async Task<IActionResult> Word()
    {
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        _client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
        
        var response = _client.GetStringAsync("https://v2.jokeapi.dev/joke/any");

        var joke = await response;
        return Ok(joke);
    }
}