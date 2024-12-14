using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/get-weather-at")]
public class WeatherController : ControllerBase
{
    private readonly WeatherService _weatherService;

    public WeatherController(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWeather(string city)
    {
        var weather = await _weatherService.GetWeather(city);
        if (weather == null)
        {
            return NotFound(new { Message = "City not found" });
        }

        return Ok(weather);
    }
}
