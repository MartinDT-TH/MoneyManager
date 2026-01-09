using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MoneyManager.Application.Interfaces;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Infrastructure.Data.Context;
using MoneyManager.Infrastructure.Services;
using System.Text;

// --- CẤU HÌNH GOOGLE CREDENTIALS ---
// Lấy đường dẫn file nằm cùng thư mục với ứng dụng đang chạy
string credentialPath = Path.Combine(Directory.GetCurrentDirectory(), "moneymanager-ocr.json");// google - credentials.json

// Kiểm tra xem file có thật sự tồn tại không (để debug lỗi)
if (!File.Exists(credentialPath))
{
    Console.WriteLine($"WARNING: Không tìm thấy file key tại: {credentialPath}");
}

// Set biến môi trường để thư viện Google tự nhận diện
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

// ------------------------------------

var builder = WebApplication.CreateBuilder(args);

// 1. DB Context
builder.Services.AddDbContext<MoneyManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

// 2. Identity
builder.Services.AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<MoneyManagerDbContext>()
    .AddDefaultTokenProviders();

// 3. Authentication with JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtSettings:Key"]!);
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// 4. REGISTER CUSTOM SERVICES
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFinancialReportService, FinancialReportService>();
builder.Services.AddScoped<ISmartInsightService, SmartInsightService>();
builder.Services.AddScoped<ISpendingForecaster, SpendingForecaster>();
// 1. Register ML Service as Singleton
// Reason: Training the model is expensive; we only want to do it once at startup.
// PredictionEngine is not thread-safe, so for high concurrency, you might need ObjectPool, 
// but for this MVP, Singleton with a lock or transient engine creation is acceptable.
builder.Services.AddSingleton<ICategorizationService, SmartCategorizationService>();

// 2. Register OCR Service
builder.Services.AddScoped<IBillScanningService, GoogleCloudBillScanningService>();


builder.Services.AddControllers(); 
builder.Services.AddEndpointsApiExplorer();

// 5. Swagger Config with Auth Button
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MoneyManager API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhập 'Bearer [Token]'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

var app = builder.Build();

// === SEED DATA ===
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Truyền ServiceProvider vào hàm static
        await DbInitializer.Initialize(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Loi khi khoi tao Database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Bật xác thực
app.UseAuthorization();  // Bật phân quyền

app.MapControllers();

app.Run();
