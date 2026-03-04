using CMS.AcademicService.Data;
using CMS.AcademicService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AcademicDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<INoticeService, NoticeService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Database already exists in Supabase — no EnsureCreated needed


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

app.UseDeveloperExceptionPage();
		app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsProduction()) app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("AcademicService starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AcademicService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
