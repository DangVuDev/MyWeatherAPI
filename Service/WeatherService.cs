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
        return null; // Xử lý khi không thể lấy dữ liệu
    }

    var content = await response.Content.ReadAsStringAsync();
    var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

    // Lấy múi giờ từ API
    int timezoneOffset = weatherData.GetProperty("city").GetProperty("timezone").GetInt32();
    var nowUtc = DateTime.UtcNow;
    var nowLocal = nowUtc.AddSeconds(timezoneOffset);

    var currentWeather = weatherData.GetProperty("list")[0];

    // Lấy thông tin thời tiết hàng ngày
    var dailyWeather = weatherData.GetProperty("list").EnumerateArray()
        .GroupBy(x => DateTime.Parse(x.GetProperty("dt_txt").GetString())
        .AddSeconds(timezoneOffset).Date) // Nhóm theo ngày địa phương
        .Select(g => new DailyWeather
        {
            Date = g.Key.ToString("dd/MM/yyyy"),
            Temp = $"{g.Min(x => x.GetProperty("main").GetProperty("temp_min").GetDecimal())} - {g.Max(x => x.GetProperty("main").GetProperty("temp_max").GetDecimal())}",
            Weather = g.First().GetProperty("weather")[0].GetProperty("description").GetString()
        })
        .Skip(1) // Bỏ ngày hiện tại
        .Take(5) // Lấy 5 ngày tiếp theo
        .ToList();

    return new WeatherResponse
    {
        City = weatherData.GetProperty("city").GetProperty("name").GetString(),
        Current = new CurrentWeather
        {
            Time = nowLocal.ToString("HH:mm, dd/MM/yyyy"),
            Temp = currentWeather.GetProperty("main").GetProperty("temp").GetDecimal(),
            Weather = currentWeather.GetProperty("weather")[0].GetProperty("description").GetString()
        },
        Hourly = weatherData.GetProperty("list").EnumerateArray()
            .Where(x =>
            {
                var forecastTimeUtc = DateTime.Parse(x.GetProperty("dt_txt").GetString());
                var forecastTimeLocal = forecastTimeUtc.AddSeconds(timezoneOffset);
                return forecastTimeLocal > nowLocal;
            })
            .Take(24)
            .Select(x =>
            {
                var forecastTimeLocal = DateTime.Parse(x.GetProperty("dt_txt").GetString()).AddSeconds(timezoneOffset);
                return new HourlyWeather
                {
                    Time = forecastTimeLocal.ToString("HH:mm"),
                    Temp = x.GetProperty("main").GetProperty("temp").GetDecimal(),
                    Weather = x.GetProperty("weather")[0].GetProperty("description").GetString()
                };
            })
            .ToList(),
        Daily = dailyWeather,
        Reminder = GenerateReminder(currentWeather)
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