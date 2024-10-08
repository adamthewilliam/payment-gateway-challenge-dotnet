using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public interface IPaymentsProcessingService
{
    Task<PostPaymentResponse?> ProcessCardPayment(PostPaymentRequest request);
}