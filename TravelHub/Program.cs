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
                           "http://localhost:5174",
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
builder.Services.AddHostedService<TravelHub.Service.TempFileCleanupService>();

var app = builder.Build();

// Tự động Apply Migration khi khởi động (quan trọng cho Render)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try 
    {
        dbContext.Database.Migrate();

        // Tự động Seed Tours nếu Database chưa đủ 6 tours
        if (dbContext.Tours.Count() < 6)
        {
            var seedTours = new List<Tour>
            {
                new Tour { Title = "Tour Trung Quốc 5N5Đ: HCM - Thượng Hải - Vô Tích", Destination = "Trung Quốc", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(5), DurationDays = 5, PriceVND = 16990000, ImageUrl = "https://images.unsplash.com/photo-1508804185872-d7bad890e092?w=800", Description = "Khám phá vẻ đẹp Trung Quốc", NumberOfBookings = 136 },
                new Tour { Title = "Tour Nhật Bản 5N4Đ: Tokyo - Phú Sĩ", Destination = "Nhật Bản", DepartureLocation = "Hà Nội", DepartureDate = DateTime.Now.AddDays(7), DurationDays = 5, PriceVND = 24990000, ImageUrl = "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=800", Description = "Ngắm hoa anh đào rực rỡ", NumberOfBookings = 41 },
                new Tour { Title = "Tour Đà Nẵng 3N2Đ: Bà Nà Hills - Hội An", Destination = "Đà Nẵng", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(2), DurationDays = 3, PriceVND = 3500000, ImageUrl = "https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800", Description = "Du lịch miền Trung trọn gói", NumberOfBookings = 32 },
                new Tour { Title = "Tour Thái Lan 4N3Đ: Bangkok - Pattaya", Destination = "Thái Lan", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(10), DurationDays = 4, PriceVND = 5990000, ImageUrl = "https://images.unsplash.com/photo-1506665531195-3566af2b4dfa?w=800", Description = "Thiên đường mua sắm", NumberOfBookings = 24 },
                new Tour { Title = "Tour Singapore 3N2Đ: Marina Bay Sands", Destination = "Singapore", DepartureLocation = "Hà Nội", DepartureDate = DateTime.Now.AddDays(15), DurationDays = 3, PriceVND = 8500000, ImageUrl = "https://images.unsplash.com/photo-1525625293386-3f8f99389edd?w=800", Description = "Đảo quốc sư tử", NumberOfBookings = 38 },
                new Tour { Title = "Tour Châu Âu 9N8Đ: Pháp - Thụy Sĩ - Ý", Destination = "Châu Âu", DepartureLocation = "Hồ Chí Minh", DepartureDate = DateTime.Now.AddDays(20), DurationDays = 9, PriceVND = 55990000, ImageUrl = "https://images.unsplash.com/photo-1499856871958-5b9627545d1a?w=800", Description = "Hành trình khám phá Châu Âu cổ kính", NumberOfBookings = 21 }
            };
            dbContext.Tours.AddRange(seedTours);
            dbContext.SaveChanges();
            Console.WriteLine("Successfully seeded initial tours.");
        }

        // Tự động Seed Destinations
        await TravelHub.Data.DataSeeder.SeedDestinationsAsync(dbContext, app.Environment.ContentRootPath);

        // Seed default Tour Guide
        await TravelHub.Data.DataSeeder.SeedDefaultTourGuideAsync(dbContext);

        // Seed UserCodes for existing users
        await TravelHub.Data.DataSeeder.SeedUserCodesAsync(dbContext);

        // Seed dummy users up to 250
        await TravelHub.Data.DataSeeder.SeedRealisticUsersAsync(dbContext);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }
}

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
app.UseStaticFiles(); // Cho phép phục vụ file tĩnh từ wwwroot (ảnh upload)

// 🟢 PHẢI THÊM DÒNG NÀY ĐỂ KÍCH HOẠT CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Đẩy người dùng bị khoá ra ngay lập tức (sau khi đã xác thực)
app.UseMiddleware<TravelHub.Middleware.BlockedUserMiddleware>();

app.MapControllers();
app.MapHub<TravelHub.Hubs.ChatHub>("/hubs/chat");

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

app.Run();