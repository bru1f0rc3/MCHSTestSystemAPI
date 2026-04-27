using MCHSWebAPI.Data;
using MCHSWebAPI.Services.AuthService.AuthService;
using MCHSWebAPI.Services.EmailService;
using MCHSWebAPI.Services.LectureService.LectureService;
using MCHSWebAPI.Services.ReportService.ReportService;
using MCHSWebAPI.Services.RoleService.RoleService;
using MCHSWebAPI.Services.TestService.TestService;
using MCHSWebAPI.Services.UserService.UserService;
using MCHSWebAPI.Services.VerificationService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 100_000;
    options.Limits.MaxRequestBodySize = 5L * 1024 * 1024 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILectureService, LectureService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<ITestingService, TestingService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPdfParserService, PdfParserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmailService, MCHSWebAPI.Services.EmailService.EmailService>();
builder.Services.AddScoped<IVerificationService, MCHSWebAPI.Services.VerificationService.VerificationService>();
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MCHSWebAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MCHSMobileApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "MCHS API";
        document.Info.Version = "v1";
        document.Info.Description = "API для тестирования МЧС";
        return Task.CompletedTask;
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
    await DatabaseInitializer.MigrateSchemaAsync(factory, logger);
    await DatabaseInitializer.EnsureDefaultAdminAsync(factory, logger);
    await DatabaseInitializer.SeedSampleDataAsync(factory, logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
var storagePath = Path.Combine(app.Environment.ContentRootPath, "storage");
if (!Directory.Exists(storagePath))
    Directory.CreateDirectory(storagePath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(storagePath),
    RequestPath = "/storage",
    ServeUnknownFileTypes = true
});

app.MapControllers();
app.MapScalarApiReference(options =>
{
    options.WithTitle("MCHS API").WithTheme(ScalarTheme.Purple);
});

app.Run();
