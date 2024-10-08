using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.RequestFilters;

public class ProcessPaymentRequestValidatorFilter : IAsyncActionFilter
{
    private static readonly string[] ValidCurrencies = ["USD", "EUR", "GBP"];
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = GetRequest(context);
        
        ValidateRequest(context, request);

        if (!IsModelStateValid(context))
        {
            // Specification says to return Rejected so I won't return any validation errors
            /*context.Result =
                new BadRequestObjectResult(context.ModelState.Where(x =>
                    x.Value?.ValidationState == ModelValidationState.Invalid));*/
            
            // Return Rejected when the request is invalid
            // We still have the errors in the context model state so we can also log the errors if we wanted to
            var response = new PostPaymentResponse { Status = PaymentStatus.Rejected };
            context.Result = new BadRequestObjectResult(response.Status);
            return;
        }

        await next();
    }

    // We can probably just return on the first error but I think it's
    // nice to have the errors added to the context in case requirements
    // change and we want to return the errors as well
    private static void ValidateRequest(ActionExecutingContext context, PostPaymentRequest request)
    {
        if (string.IsNullOrEmpty(request.CardNumber) || !Regex.IsMatch(request.CardNumber, @"^\d{14,19}$"))
        {
            context.ModelState.AddModelError(nameof(request.CardNumber), "Invalid card number");
        }

        if (request.ExpiryMonth < 1 || request.ExpiryMonth > 12)
        {
            context.ModelState.AddModelError(nameof(request.ExpiryMonth), "Invalid expiry month");
        }

        var now = DateTime.Now;

        if (request.ExpiryYear < now.Year || (request.ExpiryYear == now.Year && request.ExpiryMonth <= now.Month))
        {
            context.ModelState.AddModelError(nameof(request.ExpiryMonth), "Card has expired");
            context.ModelState.AddModelError(nameof(request.ExpiryYear), "Card has expired");
        }

        if (!ValidCurrencies.Contains(request.Currency))
        {
            context.ModelState.AddModelError(nameof(request.Currency), "Invalid currency");
        }

        if (request.Amount <= 0)
        {
            context.ModelState.AddModelError(nameof(request.Amount), "Invalid amount");
        }

        var cvv = request.Cvv.ToString();

        if (string.IsNullOrEmpty(cvv) || !Regex.IsMatch(cvv, @"^\d{3,4}$"))
        {
            context.ModelState.AddModelError(nameof(request.Cvv), "Invalid CVV");
        }
    }

    private static PostPaymentRequest GetRequest(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("request", out var requestObject) &&
            requestObject is PostPaymentRequest request)
        {
            return request;
        }

        throw new KeyNotFoundException("No request key found in context");
    }

    private static bool IsModelStateValid(ActionExecutingContext context) => context.ModelState.ValidationState == ModelValidationState.Valid;
}