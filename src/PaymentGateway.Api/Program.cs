using System.Text.Json;

using Microsoft.Extensions.Options;

using PaymentGateway.Api.Config;
using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AcquiringBankHttpClientSettings>(builder.Configuration.GetSection("AcquiringBankSettings"));

// Registered as a Singleton
builder.Services.AddHttpClient("AcquiringBank", (serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<AcquiringBankHttpClientSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddSingleton<IHttpClientService, HttpClientService>();

// Options to serialize the Acquiring bank request according to the specification
builder.Services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddScoped<IPaymentsProcessingService, PaymentsProcessingService>();

// Add SeriLog for HTTP request logging and for service/dependency logging
// Add a Health check endpoint for the API

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
