using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TravelHub.Model;
using TravelHub.Service;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CẤU HÌNH CÁC DỊCH VỤ (SERVICES)
// =========================================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", 
                           "http://localhost:5173",
                           "https://travel-hub-tau.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
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

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập 'Bearer' [space] và token của bạn vào ô bên dưới.\r\n\r\nVí dụ: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// Cấu hình Authentication & JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Configure SignalR to read the token from the query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Add SignalR
builder.Services.AddSignalR();

// Đăng ký Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient();

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
    
    // Fix Swagger UI bug with OpenAPI 3.0.4
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/swagger") && context.Request.Path.ToString().EndsWith(".json"))
        {
            var originalBodyStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await next();

            context.Response.Body = originalBodyStream;
            memoryStream.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(memoryStream).ReadToEndAsync();
            json = json.Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.1\"");
            await context.Response.WriteAsync(json);
            return;
        }
        await next();
    });

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TravelHub API v1");
    });
}

app.UseHttpsRedirection();

// 🟢 PHẢI THÊM DÒNG NÀY ĐỂ KÍCH HOẠT CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TravelHub.Hubs.ChatHub>("/hubs/chat");

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

app.Run();