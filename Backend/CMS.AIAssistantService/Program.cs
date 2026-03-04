AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
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

// Database already exists in Supabase — no EnsureCreated needed


app.Run();
