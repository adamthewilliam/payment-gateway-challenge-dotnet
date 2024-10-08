using FluentAssertions;
using Moq;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsProcessingServiceTests
{
    private readonly Mock<IHttpClientService> _mockHttpClientService;
    private readonly Mock<IPaymentsRepository> _mockPaymentsRepository;
    private readonly PaymentsProcessingService _service;

    public PaymentsProcessingServiceTests()
    {
        _mockHttpClientService = new Mock<IHttpClientService>();
        _mockPaymentsRepository = new Mock<IPaymentsRepository>();
        _service = new PaymentsProcessingService(_mockHttpClientService.Object, _mockPaymentsRepository.Object);
    }

    [Fact]
    public async Task ProcessCardPayment_Should_Return_Authorized_Response_When_AcquiringBank_Authorizes_Payment()
    {
        // Arrange
        var request = CreateSampleRequest();
        var acquiringBankResponse = new AcquiringBankResponse(true, string.Empty);
        _mockHttpClientService
            .Setup(x => x.SendPaymentRequestAsync(It.IsAny<AcquiringBankRequest>()))
            .ReturnsAsync(acquiringBankResponse);

        // Act
        var result = await _service.ProcessCardPayment(request);

        // Assert
        result.Should().NotBeNull();
        result?.Status.Should().Be(PaymentStatus.Authorized);
        result?.CardNumberLastFour.Should().Be(3456);
        result?.Currency.Should().Be(request.Currency);
        result?.Amount.Should().Be(request.Amount);
        result?.ExpiryMonth.Should().Be(request.ExpiryMonth);
        result?.ExpiryYear.Should().Be(request.ExpiryYear);

        _mockPaymentsRepository.Verify(x => x.Add(It.IsAny<PostPaymentResponse>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCardPayment_Should_Return_Declined_Response_When_AcquiringBank_Does_Not_Authorize_Payment()
    {
        // Arrange
        var request = CreateSampleRequest();
        var acquiringBankResponse = new AcquiringBankResponse(false, string.Empty );
        _mockHttpClientService
            .Setup(x => x.SendPaymentRequestAsync(It.IsAny<AcquiringBankRequest>()))
            .ReturnsAsync(acquiringBankResponse);

        // Act
        var result = await _service.ProcessCardPayment(request);

        // Assert
        result.Should().NotBeNull();
        result?.Status.Should().Be(PaymentStatus.Declined);

        _mockPaymentsRepository.Verify(x => x.Add(It.IsAny<PostPaymentResponse>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCardPayment_Should_Return_Declined_Response_When_AcquiringBank_Returns_Null()
    {
        // Arrange
        var request = CreateSampleRequest();
        _mockHttpClientService
            .Setup(x => x.SendPaymentRequestAsync(It.IsAny<AcquiringBankRequest>()))
            .ReturnsAsync((AcquiringBankResponse?)null);

        // Act
        var result = await _service.ProcessCardPayment(request);

        // Assert
        result.Should().NotBeNull();
        result?.Status.Should().Be(PaymentStatus.Declined);

        _mockPaymentsRepository.Verify(x => x.Add(It.IsAny<PostPaymentResponse>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCardPayment_Should_Send_Correctly_Formatted_Request_To_AcquiringBank()
    {
        // Arrange
        var request = CreateSampleRequest();
        _mockHttpClientService
            .Setup(x => x.SendPaymentRequestAsync(It.IsAny<AcquiringBankRequest>()))
            .ReturnsAsync(new AcquiringBankResponse(true, string.Empty));

        // Act
        await _service.ProcessCardPayment(request);

        // Assert
        _mockHttpClientService.Verify(x => x.SendPaymentRequestAsync(It.Is<AcquiringBankRequest>(r => 
            r.CardNumber == request.CardNumber &&
            r.ExpiryDate == $"{request.ExpiryMonth:D2}/{request.ExpiryYear}" &&
            r.Currency == request.Currency &&
            r.Amount == request.Amount &&
            r.Cvv == request.Cvv.ToString()
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessCardPayment_Should_Propagate_Exception_When_Repository_Throws()
    {
        // Arrange
        var request = CreateSampleRequest();
        _mockHttpClientService
            .Setup(x => x.SendPaymentRequestAsync(It.IsAny<AcquiringBankRequest>()))
            .ReturnsAsync(new AcquiringBankResponse( true, string.Empty));
        _mockPaymentsRepository
            .Setup(x => x.Add(It.IsAny<PostPaymentResponse>()))
            .Throws(new InvalidOperationException("Duplicate ID"));

        // Act & Assert
        await _service.Invoking(s => s.ProcessCardPayment(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Duplicate ID");
    }

    private PostPaymentRequest CreateSampleRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000,
            Cvv = 123
        };
    }
}