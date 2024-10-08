using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsProcessingService : IPaymentsProcessingService
{
    private readonly IHttpClientService _httpClientService;
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentsProcessingService(
        IHttpClientService httpClientService,
        IPaymentsRepository paymentsRepository)
    {
        _httpClientService = httpClientService;
        _paymentsRepository = paymentsRepository;
    }
    
    public async Task<PostPaymentResponse?> ProcessCardPayment(PostPaymentRequest request)
    {
        var acquiringBankRequest = new AcquiringBankRequest(
            CardNumber: request.CardNumber,
            ExpiryDate: $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            Currency: request.Currency,
            Amount: request.Amount,
            Cvv: request.Cvv.ToString()
        );
        
        var acquiringBankResult = await _httpClientService.SendPaymentRequestAsync(acquiringBankRequest);

        // Received an error status code so decline the request
        if (acquiringBankResult is null)
        {
            return new PostPaymentResponse
            {
                Status = PaymentStatus.Declined
            };
        }
        
        // I am going to assume that we don't want to store the full card number in our "database"
        var response = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            CardNumberLastFour = int.Parse(request.CardNumber[^4..]),
            Status = acquiringBankResult.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            Currency = request.Currency,
            Amount = request.Amount,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear
        };
        
        _paymentsRepository.Add(response);

        return response;
    }
}