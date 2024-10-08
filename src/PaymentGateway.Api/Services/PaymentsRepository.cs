using System.Collections.Concurrent;

using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository : IPaymentsRepository
{
    // Refactored to use a concurrent dictionary. Provides thread safety and faster lookup
    private readonly ConcurrentDictionary<Guid, PostPaymentResponse> _payments = new();
    
    public void Add(PostPaymentResponse payment)
    {
        if (!_payments.TryAdd(payment.Id, payment))
        {
            throw new InvalidOperationException($"A payment with ID {payment.Id} already exists.");
        }
    }

    public PostPaymentResponse? Get(Guid id)
    {
        _payments.TryGetValue(id, out var payment);
        return payment;
    }
}