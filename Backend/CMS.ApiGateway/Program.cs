// CMS.ApiGateway/Program.cs

using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using MMLib.Ocelot.Provider.AppConfiguration;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Microsoft.Extensions.Http;
using Serilog;
using Serilog.Context;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Configuration Setup
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Services Registration
builder.Services.AddOcelot(builder.Configuration)
    .AddAppConfiguration();

builder.Services.AddSwaggerForOcelot(builder.Configuration,
    (o) =>
    {
        o.GenerateDocsForGatewayItSelf = false;
    });

// Bypass SSL cert validation for downstream swagger.json fetching (dev certs)
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
    {
        options.HttpMessageHandlerBuilderActions.Add(b =>
        {
            b.PrimaryHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        });
    });
}

builder.Services.AddControllers();

// CORS Configuration - Allow frontend and Swagger access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Kestrel configuration
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    // Running in Docker/Render — use HTTP on PORT
    builder.WebHost.UseKestrel(kestrelOptions =>
    {
        kestrelOptions.ListenAnyIP(int.Parse(port));
    });
}
else
{
    // Local development — use HTTPS on 7000
    builder.WebHost.UseKestrel(kestrelOptions =>
    {
        kestrelOptions.ListenAnyIP(7000, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    });
}

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

// HTTP Request Pipeline
// IMPORTANT: CORS must be one of the first middlewares
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerForOcelotUI(opt =>
    {
        opt.PathToSwaggerGenerator = "/swagger/docs";
    });
}

if (!app.Environment.IsProduction()) app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Log.Information("ApiGateway starting up...");

try
{
    await app.UseOcelot();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ApiGateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
