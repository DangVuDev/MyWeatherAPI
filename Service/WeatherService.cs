using System.Net.Http;
using System.Text.Json;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "a761fbd01350a6d206000250af0f0679";

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherResponse> GetWeather(string city)
{
    var url = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&units=metric&appid={ApiKey}";
    var response = await _httpClient.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        return null;
    }

    var content = await response.Content.ReadAsStringAsync();
    var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

    var currentWeather = weatherData.GetProperty("list")[0];
    int remainingHours = 24 - DateTime.Now.Hour;
    var hourlyWeather = weatherData.GetProperty("list")
    .EnumerateArray()
    .Where(x =>
    {
        // Lấy thời gian từ dữ liệu API và chuyển đổi nó thành DateTime
        DateTime forecastTime = DateTime.Parse(x.GetProperty("dt_txt").GetString());
        
        // Kiểm tra xem thời gian dự báo có sau thời gian hiện tại không và không bị trùng lặp
        return forecastTime > DateTime.Now; // Lọc các giờ sau giờ hiện tại
    })
    .Take(remainingHours) // Lấy các giờ tiếp theo cho đến hết ngày
    .ToList();
    var dailyWeather = weatherData.GetProperty("list").EnumerateArray()
                                    .GroupBy(x => DateTime.Parse(x.GetProperty("dt_txt").GetString()).Date)
                                    .Select(g => g.First())
                                    .Skip(1) // Bỏ ngày hiện tại
                                    .Take(5); // 5 ngày tiếp theo

    return new WeatherResponse
    {
        City = weatherData.GetProperty("city").GetProperty("name").GetString(),
        Current = new CurrentWeather
        {
            Time = DateTime.Parse(currentWeather.GetProperty("dt_txt").GetString()).ToString("HH:mm, dd/MM/yyyy"),
            Temp = currentWeather.GetProperty("main").GetProperty("temp").GetDecimal(),
            Weather = currentWeather.GetProperty("weather")[0].GetProperty("description").GetString()
        },
        Hourly = hourlyWeather.Select(x => new HourlyWeather
        {
            Time = DateTime.Parse(x.GetProperty("dt_txt").GetString()).ToString("HH:mm"),
            Temp = x.GetProperty("main").GetProperty("temp").GetDecimal(),
            Weather = x.GetProperty("weather")[0].GetProperty("description").GetString()
        }).ToList(),
        Daily = dailyWeather
            .GroupBy(x => x.GetProperty("dt_txt").GetString().Split(" ")[0]) // Nhóm theo ngày
            .Select(group => new DailyWeather
            {
                Date = group.Key,
                Temp = $"{group.Min(x => x.GetProperty("main").GetProperty("temp").GetDecimal())} - {group.Max(x => x.GetProperty("main").GetProperty("temp").GetDecimal())}",
                Weather = group.First().GetProperty("weather")[0].GetProperty("description").GetString()
            }).ToList(),
        Reminder = GenerateReminder(currentWeather) // Logic nhắc nhở thời tiết
    };
}
private string GenerateReminder(JsonElement currentWeather)
{
    var description = currentWeather.GetProperty("weather")[0].GetProperty("description").GetString();
    if (description.Contains("rain"))
    {
        return "Hãy mang ô vì trời mưa!";
    }
    else if (description.Contains("clear"))
    {
        return "Hôm nay trời quang đãng, hãy tận hưởng!";
    }
    return "Hãy kiểm tra thời tiết trước khi ra ngoài.";
}


}
public class WeatherResponse
{
    public string City { get; set; }
    public CurrentWeather Current { get; set; }
    public IEnumerable<HourlyWeather> Hourly { get; set; }
    public IEnumerable<DailyWeather> Daily { get; set; }
    public string Reminder { get; set; }
}

public class CurrentWeather
{
    public string Time { get; set; }
    public decimal Temp { get; set; }
    public string Weather { get; set; }
}

public class HourlyWeather
{
    public string Time { get; set; }
    public decimal Temp { get; set; }
    public string Weather { get; set; }
}

public class DailyWeather
{
    public string Date { get; set; }
    public string Temp { get; set; }
    public string Weather { get; set; }
}