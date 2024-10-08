using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.RequestFilters;

public class ProcessPaymentRequestValidatorAttribute() : TypeFilterAttribute(typeof(ProcessPaymentRequestValidatorFilter));