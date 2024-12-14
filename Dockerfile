# Sử dụng hình ảnh nền chính thức của .NET SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Sao chép file dự án và khôi phục các dependencies
COPY *.csproj ./
RUN dotnet restore

# Sao chép toàn bộ mã nguồn
COPY . ./

# Build ứng dụng trong chế độ Release
RUN dotnet publish -c Release -o out

# Sử dụng hình ảnh .NET Runtime để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Sao chép ứng dụng đã build từ giai đoạn trước
COPY --from=build /app/out ./

# Chạy ứng dụng khi container khởi động
ENTRYPOINT ["dotnet", "MyWeatherAPI.dll"]