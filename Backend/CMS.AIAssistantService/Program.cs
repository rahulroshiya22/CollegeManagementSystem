using CMS.AIAssistantService.Data;
using CMS.AIAssistantService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure HttpClient for service integration
builder.Services.AddHttpClient<ServiceIntegrationService>();

// Register services
builder.Services.AddScoped<GeminiAIService>();
builder.Services.AddScoped<ChatHistoryService>();
builder.Services.AddScoped<ServiceIntegrationService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsProduction()) app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
