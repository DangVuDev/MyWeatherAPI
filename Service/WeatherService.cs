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

    // Lấy múi giờ từ API
    int timezoneOffset = weatherData.GetProperty("city").GetProperty("timezone").GetInt32();
    var nowUtc = DateTime.UtcNow;
    var nowLocal = nowUtc.AddSeconds(timezoneOffset); // Chuyển sang giờ địa phương

    var currentWeather = weatherData.GetProperty("list")[0];

    // Lấy thông tin thời tiết theo từng giờ cho đến hết ngày
    var hourlyWeather = weatherData.GetProperty("list")
        .EnumerateArray()
        .Where(x =>
        {
            // Lấy thời gian từ API và chuyển đổi sang giờ địa phương
            var forecastTimeUtc = DateTime.Parse(x.GetProperty("dt_txt").GetString());
            var forecastTimeLocal = forecastTimeUtc.AddSeconds(timezoneOffset);

            // Chỉ lấy các dự báo từ thời gian hiện tại
            return forecastTimeLocal > nowLocal;
        })
        .Take(24) // Lấy dự báo cho 24 giờ tiếp theo (nếu có)
        .ToList();

    // Lấy thông tin thời tiết hàng ngày
    var dailyWeather = weatherData.GetProperty("list").EnumerateArray()
        .GroupBy(x => DateTime.Parse(x.GetProperty("dt_txt").GetString())
        .AddSeconds(timezoneOffset).Date) // Nhóm theo ngày địa phương
        .Select(g => g.First())
        .Skip(1) // Bỏ ngày hiện tại
        .Take(5); // Lấy 5 ngày tiếp theo

    return new WeatherResponse
    {
        City = weatherData.GetProperty("city").GetProperty("name").GetString(),
        Current = new CurrentWeather
        {
            Time = nowLocal.ToString("HH:mm, dd/MM/yyyy"),
            Temp = currentWeather.GetProperty("main").GetProperty("temp").GetDecimal(),
            Weather = currentWeather.GetProperty("weather")[0].GetProperty("description").GetString()
        },
        Hourly = hourlyWeather.Select(x =>
        {
            var forecastTimeLocal = DateTime.Parse(x.GetProperty("dt_txt").GetString()).AddSeconds(timezoneOffset);
            return new HourlyWeather
            {
                Time = forecastTimeLocal.ToString("HH:mm"), // Hiển thị theo giờ địa phương
                Temp = x.GetProperty("main").GetProperty("temp").GetDecimal(),
                Weather = x.GetProperty("weather")[0].GetProperty("description").GetString()
            };
        }).ToList(),
        Daily = dailyWeather.Select(x =>
        {
            var dateLocal = DateTime.Parse(x.GetProperty("dt_txt").GetString()).AddSeconds(timezoneOffset).Date;
            return new DailyWeather
            {
                Date = dateLocal.ToString("dd/MM/yyyy"),
                Temp = $"{x.GetProperty("main").GetProperty("temp_min").GetDecimal()} - {x.GetProperty("main").GetProperty("temp_max").GetDecimal()}",
                Weather = x.GetProperty("weather")[0].GetProperty("description").GetString()
            };
        }).ToList(),
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