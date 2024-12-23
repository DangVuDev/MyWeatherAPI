using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);

// Thêm WeatherService vào dependency injection
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddControllers(); // Thêm Controllers vào pipeline
// Thêm dịch vụ CORS vào container DI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("https://myweatherweb.onrender.com") // Địa chỉ frontend React.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
var app = builder.Build();
app.UseCors("AllowReactApp");

// Sử dụng Controllers
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
