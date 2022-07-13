using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace RandomWord.Controllers;

[ApiController]
[Route("[controller]")]
public class RandomWordController : ControllerBase
{
    private readonly HttpClient _client;

    public RandomWordController(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient();
    }

    [HttpGet]
    public async Task<IActionResult> Word()
    {
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        _client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
        
        var response = _client.GetStringAsync("https://random-word-api.herokuapp.com/word");

        var word = await response;
        return Ok(word);
    }
}