using System.Text.Json;

using Microsoft.Extensions.Options;

using PaymentGateway.Api.Config;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class HttpClientService : IHttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    
    private readonly IOptions<AcquiringBankHttpClientSettings> _settings;

    public HttpClientService(
        IHttpClientFactory httpClientFactory,
        JsonSerializerOptions jsonSerializerOptions,
        IOptions<AcquiringBankHttpClientSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _jsonSerializerOptions = jsonSerializerOptions; 
        _settings = settings;
    }

    public async Task<AcquiringBankResponse?> SendPaymentRequestAsync(AcquiringBankRequest request)
    {
        var client = _httpClientFactory.CreateClient("AcquiringBank");
        
        // For debugging
        //var jsonRequest = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        try
        {
            var response = await client.PostAsJsonAsync(_settings.Value.BaseUrl, request, _jsonSerializerOptions);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AcquiringBankResponse?>();
            }

            // If the request is not successful, return null and treat it as declined
            return null;
        }
        catch (HttpRequestException exception)
        {
            // Log the exception, return null and treat it as declined
            return null;
        }
    }
}