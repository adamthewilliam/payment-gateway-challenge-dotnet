using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using FluentAssertions;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.RequestFilters;
using Moq;

using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Tests;

public class ProcessPaymentRequestValidatorFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_ValidRequest_Should_Call_Next_Delegate()
    {
        // Arrange
        var filter = new ProcessPaymentRequestValidatorFilter();
        
        var validRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = 123
        };
        
        var context = CreateContext(validRequest);
        var nextCalled = false;
        
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(default!);
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Theory]
    [InlineData("123", "Invalid card number")]
    [InlineData("12345678901234567890", "Invalid card number")]
    [InlineData("1234abcd5678efgh", "Invalid card number")]
    public async Task OnActionExecutionAsync_Invalid_Card_Number_Should_Return_Bad_Request_With_Rejected_Status(string cardNumber, string expectedError)
    {
        // Arrange
        var filter = new ProcessPaymentRequestValidatorFilter();
        
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = 123
        };
        
        var context = CreateContext(invalidRequest);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(default!));

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be(PaymentStatus.Rejected);
        context.ModelState["CardNumber"]?.Errors.Should().Contain(e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task OnActionExecutionAsync_Invalid_Expiry_Month_Should_Return_Bad_Request_With_Rejected_Status(int expiryMonth)
    {
        // Arrange
        var filter = new ProcessPaymentRequestValidatorFilter();
        
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = expiryMonth,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = 123
        };
        
        var context = CreateContext(invalidRequest);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(default!));

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be(PaymentStatus.Rejected);
        context.ModelState["ExpiryMonth"]?.Errors.Should().Contain(e => e.ErrorMessage == "Invalid expiry month");
    }

    [Fact]
    public async Task OnActionExecutionAsync_Expired_Card_Should_Return_With_Bad_Request_With_Rejected_Status()
    {
        // Arrange
        var filter = new ProcessPaymentRequestValidatorFilter();
        
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = DateTime.Now.Month,
            ExpiryYear = DateTime.Now.Year - 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = 123
        };
        
        var context = CreateContext(invalidRequest);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(default!));

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be(PaymentStatus.Rejected);
        context.ModelState["ExpiryMonth"]?.Errors.Should().Contain(e => e.ErrorMessage == "Card has expired");
        context.ModelState["ExpiryYear"]?.Errors.Should().Contain(e => e.ErrorMessage == "Card has expired");
    }

    [Fact]
    public async Task OnActionExecutionAsync_Invalid_Currency_Should_Return_Bad_Request_With_Rejected_Status()
    {
        // Arrange
        var filter = new ProcessPaymentRequestValidatorFilter();
        
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "ABC",
            Amount = 1000,
            Cvv = 123
        };
        
        var context = CreateContext(invalidRequest);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(default!));

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be(PaymentStatus.Rejected);
        context.ModelState["Currency"]?.Errors.Should().Contain(e => e.ErrorMessage == "Invalid currency");
    }

    [Fact]
    public async Task OnActionExecutionAsync_InvalidAmount_Should_Return_Bad_Request_With_Rejected_Status()
    {
        // Arrange
        var filter = new ProcessPaymentRequestValidatorFilter();
        
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "USD",
            Amount = 0,
            Cvv = 123
        };
        var context = CreateContext(invalidRequest);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(default!));

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be(PaymentStatus.Rejected);
        context.ModelState["Amount"]?.Errors.Should().Contain(e => e.ErrorMessage == "Invalid amount");
    }

    [Theory]
    [InlineData(12)]
    [InlineData(12345)]
    public async Task OnActionExecutionAsync_Invalid_Cvv_Should_Return_Bad_Request_With_Rejected_Status(int cvv)
    {
        // Arrange
        var filter = new ProcessPaymentRequestValidatorFilter();
        
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.Now.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = cvv
        };
        var context = CreateContext(invalidRequest);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(default!));

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be(PaymentStatus.Rejected);
        context.ModelState["Cvv"]?.Errors.Should().Contain(e => e.ErrorMessage == "Invalid CVV");
    }
    
    private ActionExecutingContext CreateContext(PostPaymentRequest request)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { { "request", request } },
            new Mock<Controller>().Object);
    }
}