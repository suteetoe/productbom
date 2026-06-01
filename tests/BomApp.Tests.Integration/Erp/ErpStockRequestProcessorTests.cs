using System.Net;
using System.Text.Json;
using BomApp.Application.Configuration;
using BomApp.Application.Interfaces;
using BomApp.Infrastructure.Erp;
using FluentAssertions;
using Moq;

namespace BomApp.Tests.Integration.Erp;

public class ErpStockRequestProcessorTests
{
    [Fact]
    public async Task ProcessStockRequestAsync_PostsExpectedPayloadToErpEndpoint()
    {
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler);
        var runtimeConfiguration = new Mock<IRuntimeConfigurationService>();
        runtimeConfiguration
            .SetupGet(s => s.Current)
            .Returns(new RuntimeAppSettings
            {
                ErpWebServiceUrl = "http://erp.example.test",
                ProviderCode = "IMEXERPPOC",
                DatabaseConnection = new DatabaseConnectionSettings
                {
                    DatabaseName = "imexpocdata"
                }
            });

        var processor = new ErpStockRequestProcessor(httpClient, runtimeConfiguration.Object);

        await processor.ProcessStockRequestAsync(["04000-IS4HF", "04000-IS6BB1", "04000-IS4HF"]);

        handler.RequestUri.Should().Be("http://erp.example.test/SMLJavaWebService/rest/v1/processstockrequest");
        handler.ContentType.Should().Be("application/json; charset=utf-8");

        using var json = JsonDocument.Parse(handler.Body);
        json.RootElement.GetProperty("providerCode").GetString().Should().Be("IMEXERPPOC");
        json.RootElement.GetProperty("databaseName").GetString().Should().Be("imexpocdata");
        json.RootElement.GetProperty("itemCode").EnumerateArray().Select(x => x.GetString()).Should()
            .Equal("04000-IS4HF", "04000-IS6BB1");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public string? ContentType { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            ContentType = request.Content?.Headers.ContentType?.ToString();
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
