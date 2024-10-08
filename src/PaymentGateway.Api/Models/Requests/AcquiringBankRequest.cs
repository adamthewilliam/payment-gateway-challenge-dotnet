namespace PaymentGateway.Api.Models.Requests;

public record AcquiringBankRequest(
    string CardNumber,
    string ExpiryDate,
    string Currency,
    int Amount,
    string Cvv
);