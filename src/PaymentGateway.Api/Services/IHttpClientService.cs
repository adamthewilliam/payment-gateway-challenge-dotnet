using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public interface IHttpClientService
{
    Task<AcquiringBankResponse?> SendPaymentRequestAsync(AcquiringBankRequest request);
}