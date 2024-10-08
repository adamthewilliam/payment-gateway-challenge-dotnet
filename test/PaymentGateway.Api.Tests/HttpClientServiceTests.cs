using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PaymentGateway.Api.Config;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class HttpClientServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IOptions<AcquiringBankHttpClientSettings> _settings;

    public HttpClientServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _jsonSerializerOptions = new JsonSerializerOptions();
        _settings = Options.Create(new AcquiringBankHttpClientSettings { BaseUrl = "http://test.com" });
    }

    [Fact]
    public async Task When_SendPaymentRequestAsync_Sends_SuccessfulRequest_Then_AcquiringBankResponse_With_Authorized_True_Should_Be_Returned()
    {
        // Arrange
        var expectedResponse = new AcquiringBankResponse(true, string.Empty);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
        };
        CreateHttpClient(responseMessage);

        var service = new HttpClientService(_mockHttpClientFactory.Object, _jsonSerializerOptions, _settings);
        var request = new AcquiringBankRequest("1234", "12/25", "USD", 1000, "123");

        // Act
        var result = await service.SendPaymentRequestAsync(request);

        // Assert
        result.Should().NotBeNull();
        result?.Authorized.Should().BeTrue();
    }

    [Fact]
    public async Task When_SendPaymentRequestAsync_Sends_UnsuccessfulRequest_Null_Should_Be_Returned()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
        CreateHttpClient(responseMessage);

        var service = new HttpClientService(_mockHttpClientFactory.Object, _jsonSerializerOptions, _settings);
        var request = new AcquiringBankRequest("1234", "12/25", "USD", 1000, "123");

        // Act
        var result = await service.SendPaymentRequestAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task When_SendPaymentRequestAsync_Catches_An_HttpRequestException_Then_Null_Should_Be_Returned()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        var client = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient("AcquiringBank")).Returns(client);

        var service = new HttpClientService(_mockHttpClientFactory.Object, _jsonSerializerOptions, _settings);
        var request = new AcquiringBankRequest("1234", "12/25", "USD", 1000, "123");

        // Act
        var result = await service.SendPaymentRequestAsync(request);

        // Assert
        result.Should().BeNull();
    }
    
    private void CreateHttpClient(HttpResponseMessage response)
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var client = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient("AcquiringBank")).Returns(client);
    }
}