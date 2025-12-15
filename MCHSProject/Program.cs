using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MCHSProject.ConnectionDB;
using MCHSProject.Common.Middleware;
using MCHSProject.Services.Users;
using MCHSProject.Services.Lectures;
using MCHSProject.Services.Tests;
using MCHSProject.Services.Questions;
using MCHSProject.Services.Answers;
using MCHSProject.Services.TestResults;
using MCHSProject.Services.Reports;
using MCHSProject.Services.Roles;
using MCHSProject.Services.Paths;
using MCHSProject.Services.Documents;
using MCHSProject.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

// Services
builder.Services.AddScoped<DBConnect>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LectureService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<AnswerService>();
builder.Services.AddScoped<TestResultService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PathService>();
builder.Services.AddScoped<DocumentParserService>();

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCHS Project API",
        Version = "v1",
        Description = "API для мобильного приложения MCHS Project"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите JWT токен"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS for mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCHS Project API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("MobileApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
