using CMS.EnrollmentService.Data;
using CMS.EnrollmentService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Context;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Service
builder.Services.AddScoped<IEnrollmentService, EnrollmentManagementService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Correlation ID Middleware
app.Use(async (context, next) =>
{
    var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault()
                   ?? Guid.NewGuid().ToString("N");
    
    using (LogContext.PushProperty("TraceId", traceId))
    {
        context.Response.Headers["X-Trace-Id"] = traceId;
        await next();
    }
});

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsProduction()) app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("EnrollmentService starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EnrollmentService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
