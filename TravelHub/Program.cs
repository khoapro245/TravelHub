using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TravelHub.Model;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CẤU HÌNH CÁC DỊCH VỤ (SERVICES)
// =========================================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TravelHub API",
        Version = "v1",
        Description = "Hệ thống API backend dành cho ứng dụng du lịch TravelHub (.NET 10)"
    });
});

// Đăng ký DbContext kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// =========================================================================
// 2. CẤU HÌNH ĐƯỜNG ĐI REQUEST (MIDDLEWARE PIPELINE)
// =========================================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TravelHub API v1");
    });
}

app.UseHttpsRedirection();

// 🟢 PHẢI THÊM DÒNG NÀY ĐỂ KÍCH HOẠT CORS
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();