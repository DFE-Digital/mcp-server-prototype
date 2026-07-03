using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Helpers;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Dfe.Mcp.Server.Domain;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dfe.Mcp.Server.Application.Services;
 
public class DatabricksSqlService(ILogger<DatabricksSqlService> logger, HttpClient httpClient, DatabricksConfiguration databricksConfiguration)
    : IDatabricksSqlService
{
    private const string EstablishmentTableName = "rsd_catalog_30_bronze.manual_data_loading_area.dr_suess_establishment";

    public async Task<ResponseModel> RunEstablishmentQueryAsync(EstablishmentDatabricksQueryModel model, CancellationToken cancellationToken = default)
    { 
        try
        {
            var whereClause = BuildFilter(model);
            var query = $"SELECT * FROM {EstablishmentTableName} {whereClause} LIMIT {model.Limit}";

            var body = new
            {
                warehouse_id = databricksConfiguration.WarehouseId,
                statement = query,
                wait_timeout = databricksConfiguration.WaitTimeOut
            };

            var payload = JsonHelper.Serialize(body);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/2.0/sql/statements")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", databricksConfiguration.AccessToken);

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = $"Databricks returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}";
                logger.LogError("{Error}", error);
                throw new HttpRequestException(error);
            }

            var statementId = GetStatementId(await response.Content.ReadAsStringAsync(cancellationToken));
            return await PollUntilDoneAsync(statementId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running Databricks query: {Query}", JsonHelper.Serialize(model));
            return new ResponseModel(Error: ex.Message);
        }
    }
    private static string? GetStatementId(string response)
        => JsonDocument.Parse(response).RootElement.GetProperty("statement_id").GetString();

    private static string BuildFilter(EstablishmentDatabricksQueryModel model)
    {
        var conditions = new List<string>();

        void AddIfPresent(string column, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var escapedValue = value.Replace("'", "''");
                conditions.Add($"{column} = '{escapedValue}'");
            }
        }

        AddIfPresent("group_id", model.GroupId);
        AddIfPresent("urn", model.Urn);
        AddIfPresent("ukprn", model.UKPRN);
        AddIfPresent("establishment_name", model.EstablishmentName);
        AddIfPresent("establishment_phase", model.EstablishmentPhase);
        AddIfPresent("trust_name", model.TrustName);
        
        return conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;
    }

    private async Task<ResponseModel> PollUntilDoneAsync(string? statementId, CancellationToken cancellationToken, IEnumerable<string>? fields = null)
    {
        ArgumentNullException.ThrowIfNull(statementId);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(databricksConfiguration.QueryTimeoutSeconds));

        while (true)
        {
            timeoutCts.Token.ThrowIfCancellationRequested();

            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/2.0/sql/statements/{statementId}");
            pollRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", databricksConfiguration.AccessToken);

            var response = await httpClient.SendAsync(pollRequest, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                throw new InvalidOperationException(
                    $"Databricks API returned {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
            }

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeoutCts.Token));
            var root = doc.RootElement;
            var state = root.GetProperty("status").GetProperty("state").GetString();

            switch (state)
            {
                case QueryState.Succeeded:
                    return ExtractDataArray(root, fields);

                case QueryState.Failed:
                case QueryState.Canceled:
                case QueryState.Closed:
                    var errorMsg = root.TryGetProperty("status", out var s) &&
                                   s.TryGetProperty("error", out var e)
                        ? e.GetProperty("message").GetString()
                        : "Unknown error";
                    throw new InvalidOperationException($"Query {state}: {errorMsg}");

                case QueryState.Pending:
                case QueryState.Running:
                    await Task.Delay(databricksConfiguration.PollIntervalMs, timeoutCts.Token);
                    continue;

                default:
                    throw new InvalidOperationException($"Unexpected state: {state}");
            }
        }
    }
    private static ResponseModel ExtractDataArray(JsonElement root, IEnumerable<string>? fields = null)
    {
        try
        {
            if (!root.TryGetProperty("result", out var result))
            {
                return new ResponseModel(Results: "[]", TotalCount: 0);
            }

            long? totalCount = result.TryGetProperty("row_count", out var rowCountElement)
                && rowCountElement.ValueKind == JsonValueKind.Number
                    ? rowCountElement.GetInt64()
                    : 0;

            if (!result.TryGetProperty("data_array", out var dataArray))
            {
                return new ResponseModel(Results: "[]", TotalCount: totalCount);
            }

            HashSet<string>? fieldFilter = fields?.Any() == true
                ? new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase)
                : null;

            var outputArray = BuildOutputArray(dataArray, fieldFilter);

            return new ResponseModel(
                Results: outputArray.ToJsonString(),
                TotalCount: totalCount);
        }
        catch (Exception ex)
        {
            return new ResponseModel(Error: ex.Message);
        }
    }

    private static JsonArray BuildOutputArray(JsonElement dataArray, HashSet<string>? fieldFilter)
    {
        var outputArray = new JsonArray();

        foreach (var row in dataArray.EnumerateArray())
        {
            var rowObject = BuildRowObject(row, fieldFilter);
            if (rowObject != null)
            {
                outputArray.Add(rowObject);
            }
        }

        return outputArray;
    }

    private static JsonObject? BuildRowObject(JsonElement row, HashSet<string>? fieldFilter)
    {
        var rowItems = row.EnumerateArray().ToList();

        string? headerString = FindHeaderString(rowItems);
        if (headerString == null)
            return null;

        var fieldNames = headerString.Split('|');
        var jsonObject = new JsonObject();

        for (int i = 0; i < fieldNames.Length && i < rowItems.Count; i++)
        {
            var fieldName = fieldNames[i];

            if (fieldFilter != null && !fieldFilter.Contains(fieldName))
                continue;

            jsonObject[fieldName] = ConvertToJsonNode(rowItems[i]);
        }

        return jsonObject;
    }

    private static string? FindHeaderString(List<JsonElement> rowItems)
    {
        return rowItems
            .FirstOrDefault(x => x.ValueKind == JsonValueKind.String &&
                                  x.GetString()?.Contains('|') == true &&
                                  x.GetString()!.Contains("urn"))
            .GetString();
    }

    private static JsonNode? ConvertToJsonNode(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => JsonValue.Create(value.GetString()),
        JsonValueKind.Number => JsonNode.Parse(value.GetRawText()),
        JsonValueKind.True => JsonValue.Create(true),
        JsonValueKind.False => JsonValue.Create(false),
        JsonValueKind.Null => null,
        _ => JsonNode.Parse(value.GetRawText())
    };
}
