using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;

namespace BomApp.Infrastructure.Erp;

public sealed class ErpStockRequestProcessor(
    HttpClient httpClient,
    IRuntimeConfigurationService runtimeConfigurationService)
    : IErpStockRequestProcessor
{
    private const string ProcessStockRequestPath = "SMLJavaWebService/rest/v1/processstockrequest";

    public async Task ProcessStockRequestAsync(
        IReadOnlyList<string> itemCodes,
        CancellationToken ct = default)
    {
        var distinctItemCodes = itemCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinctItemCodes.Length == 0)
            return;

        var settings = runtimeConfigurationService.Current;
        if (string.IsNullOrWhiteSpace(settings.ErpWebServiceUrl))
            throw new InvalidOperationException("ยังไม่ได้ตั้งค่า ERP Web Service URL");

        if (string.IsNullOrWhiteSpace(settings.ProviderCode))
            throw new InvalidOperationException("ยังไม่ได้ตั้งค่า Provider Code");

        if (string.IsNullOrWhiteSpace(settings.DatabaseConnection.DatabaseName))
            throw new InvalidOperationException("ยังไม่ได้ตั้งค่า Database Name");

        var endpoint = new Uri(
            $"{settings.ErpWebServiceUrl.TrimEnd('/')}/{ProcessStockRequestPath}",
            UriKind.Absolute);

        var request = new ProcessStockRequestPayload(
            ProviderCode: settings.ProviderCode,
            DatabaseName: settings.DatabaseConnection.DatabaseName,
            ItemCode: distinctItemCodes);

        using var response = await httpClient.PostAsJsonAsync(endpoint, request, ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ProcessStockRequestPayload(
        [property: JsonPropertyName("providerCode")] string ProviderCode,
        [property: JsonPropertyName("databaseName")] string DatabaseName,
        [property: JsonPropertyName("itemCode")] IReadOnlyList<string> ItemCode);
}
