namespace PaymentGateway.Api.Models.Responses;

public record AcquiringBankResponse(
    bool Authorized,
    string AuthorizationCode);