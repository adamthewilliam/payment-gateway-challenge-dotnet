using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.RequestFilters;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IPaymentsProcessingService _paymentsProcessingService;

    public PaymentsController(
        IPaymentsProcessingService paymentsProcessingService,
        IPaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
        _paymentsProcessingService = paymentsProcessingService;
    }

    // If I had more time, I would move extract this business logic into it's own service
    // This endpoint is pretty simple though
    [HttpGet("{id:guid:required}", Name = nameof(GetPaymentAsync))]
    [ProducesResponseType<PostPaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment is null)
        {
            return new NotFoundObjectResult($"Payment details not found for id: {id}");
        }

        return new OkObjectResult(payment);
    }

    // Could also return a 204 here but I have left it as a 200 because we aren't creating anything
    [ProcessPaymentRequestValidator]
    [HttpPost(Name = nameof(ProcessPaymentAsync))]
    [ProducesResponseType<PostPaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostPaymentResponse?>> ProcessPaymentAsync(PostPaymentRequest request)
    {
        var result = await _paymentsProcessingService.ProcessCardPayment(request);

        if (result?.Status is PaymentStatus.Declined)
        {
            return new OkObjectResult(result.Status);
        }

        return new OkObjectResult(result);
    }
}