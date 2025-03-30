using LegalChatbot.API.Repositories;
using LegalChatbot.API.Services;
using LegalChatbot.API.Services.LLM;
using Microsoft.OpenApi.Models;
using OpenAI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins",
        corsBuilder =>
        {
            corsBuilder.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials()
                   .SetIsOriginAllowedToAllowWildcardSubdomains();
        });
});

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Legal Chatbot API", Version = "v1" });
});

// Configure LLM service - Either OpenAI or Groq
if (bool.Parse(builder.Configuration["GroqSettings:Enabled"] ?? "false"))
{
    Console.WriteLine("Configuring Groq LLM service with llama-3.3-70b-versatile model");
    
    // Configure OpenAI client to use Groq's API (they share the same API format)
    builder.Services.AddOpenAIService(settings =>
    {
        settings.ApiKey = builder.Configuration["GroqSettings:ApiKey"] ?? "gsk-dummy-key";
        settings.BaseDomain = builder.Configuration["GroqSettings:ApiUrl"] ?? "https://api.groq.com/openai/v1";
        settings.Organization = string.Empty; // Not needed for Groq
    });
}
else
{
    // Use the original OpenAI configuration
    Console.WriteLine("Configuring OpenAI service");
    
    builder.Services.AddOpenAIService(settings =>
    {
        settings.ApiKey = builder.Configuration["OpenAISettings:ApiKey"] ?? "sk-dummy-key";
        
        if (bool.Parse(builder.Configuration["OpenAISettings:UseLocalModel"] ?? "false"))
        {
            settings.BaseDomain = builder.Configuration["OpenAISettings:LocalModelUrl"] ?? "http://localhost:11434";
        }
    });
}

// Register services
builder.Services.AddSingleton<ILegalDocumentRepository, InMemoryLegalDocumentRepository>();
builder.Services.AddScoped<ILLMService, LegalChatbotAIService>();
builder.Services.AddScoped<IChatRAGService, ChatRAGService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS before HTTPS redirection
app.UseCors("AllowedOrigins");

// Configure HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
