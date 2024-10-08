using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

// Refactored the test class to use IClassFixture to prevent setup of WebApplicationFactory for every test
public class PaymentsControllerTests : IClassFixture<WebApplicationFactory<PaymentsController>>
{
    private readonly WebApplicationFactory<PaymentsController> _factory;
    private static readonly Guid TestGuid = new("12345678-1234-1234-1234-123456789012");

    public PaymentsControllerTests(WebApplicationFactory<PaymentsController> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPaymentAsync_Should_Return_OK_With_Payment_Details_When_Payment_Details_Exist()
    {
        // Arrange
        var payment = CreatePayment();
        var client = CreateClientWithMockedRepository(payment);

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paymentResponse.Should().NotBeNull();
        paymentResponse.Should().BeEquivalentTo(payment);
    }

    [Fact]
    public async Task GetPaymentAsync_Should_Return_NotFound_When_Payment_Details_Do_Not_Exist()
    {
        // Arrange
        var client = CreateClientWithMockedRepository(null);

        // Act
        var response = await client.GetAsync($"/api/Payments/{TestGuid}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Payment details not found for id: {TestGuid}");
    }

    [Fact]
    public async Task ProcessPaymentAsync_Should_Return_OK_With_Authorized_Status_When_Payment_Is_Authorized()
    {
        // Arrange
        var request = CreatePaymentRequest();
        var authorizedResponse = CreatePaymentResponse(PaymentStatus.Authorized);
        var client = CreateClientWithMockedServices(authorizedResponse);

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paymentResponse.Should().NotBeNull();
        paymentResponse?.Status.Should().Be(PaymentStatus.Authorized);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Should_Return_OK_With_Declined_Status_When_Payment_Is_Declined()
    {
        // Arrange
        var request = CreatePaymentRequest();
        var declinedResponse = CreatePaymentResponse(PaymentStatus.Declined);
        var client = CreateClientWithMockedServices(declinedResponse);

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentStatus>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paymentResponse.Should().Be(PaymentStatus.Declined);
    }
    
    [Fact]
    public async Task ProcessPaymentAsync_Should_Return_BadRequest_With_Rejected_Status_When_Validation_Fails()
    {
        // Arrange
        var invalidRequest = new PostPaymentRequest
        {
            CardNumber = "123",
            ExpiryMonth = 13,
            ExpiryYear = 2020,
            Currency = "INVALID",
            Amount = 0,
            Cvv = 12345
        };
        
        var client = CreateClientWithMockedServices(default!);

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidRequest);
        PaymentStatus content = await response.Content.ReadFromJsonAsync<PaymentStatus>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Be(PaymentStatus.Rejected);
    }

    // Moq setup for returning a specific response from the repository
    private HttpClient CreateClientWithMockedRepository(PostPaymentResponse? payment)
    {
        var mockRepository = new Mock<IPaymentsRepository>();
        mockRepository.Setup(repo => repo.Get(It.IsAny<Guid>())).Returns(payment);

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockRepository.Object);
            });
        }).CreateClient();
    }

    // Moq setup for returning a specific response fromm the payment processing service
    private HttpClient CreateClientWithMockedServices(PostPaymentResponse response)
    {
        var mockProcessingService = new Mock<IPaymentsProcessingService>();
        mockProcessingService.Setup(service => service.ProcessCardPayment(It.IsAny<PostPaymentRequest>()))
            .ReturnsAsync(response);

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockProcessingService.Object);
            });
        }).CreateClient();
    }

    private static PostPaymentResponse CreatePayment()
    {
        return new PostPaymentResponse
        {
            Id = TestGuid,
            ExpiryYear = 2025,
            ExpiryMonth = 12,
            Amount = 1000,
            CardNumberLastFour = 1234,
            Currency = "GBP",
            Status = PaymentStatus.Authorized
        };
    }

    private static PostPaymentRequest CreatePaymentRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = 123
        };
    }

    private static PostPaymentResponse CreatePaymentResponse(PaymentStatus status)
    {
        var response = CreatePayment();
        response.Status = status;
        return response;
    }
}