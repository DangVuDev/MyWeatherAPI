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
            policy.WithOrigins("http://localhost:3000") // Địa chỉ frontend React
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
