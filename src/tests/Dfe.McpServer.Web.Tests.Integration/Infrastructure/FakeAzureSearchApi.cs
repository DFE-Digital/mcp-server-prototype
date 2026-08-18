using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;

/// <summary>
/// Stands in for the Azure AI Search REST API at the HTTP transport boundary.
///
/// The real <see cref="Dfe.Mcp.Server.Application.Services.AzureSearchService"/> and the real
/// Azure SDK pipeline run against this, so index routing, <c>SearchOptions</c> translation, the
/// outgoing REST request and response deserialisation are all genuinely exercised.
/// </summary>
public sealed partial class FakeAzureSearchApi : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _responsesByIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SearchRequest> _requests = [];

    /// <summary>Every search request the service issued, in order.</summary>
    public IReadOnlyList<SearchRequest> Requests => _requests;

    /// <summary>The single search request the service issued. Fails if there was not exactly one.</summary>
    public SearchRequest SingleRequest => Assert.Single(_requests);

    /// <summary>Serves <paramref name="documents"/> for the given index name.</summary>
    public FakeAzureSearchApi RespondWith(string indexName, long totalCount, params SearchHit[] documents)
    {
        var payload = new StringBuilder().Append("{\"@odata.count\":").Append(totalCount).Append(",\"value\":[");

        payload.Append(string.Join(",", documents.Select(document =>
        {
            var fields = document.Fields.Select(field =>
                $"{JsonSerializer.Serialize(field.Key)}:{JsonSerializer.Serialize(field.Value)}");

            return $"{{\"@search.score\":{document.Score.ToString(System.Globalization.CultureInfo.InvariantCulture)},{string.Join(",", fields)}}}";
        })));

        payload.Append("]}");

        return RespondWithRawJson(indexName, payload.ToString());
    }

    /// <summary>Serves a verbatim Azure Search JSON payload for the given index name.</summary>
    public FakeAzureSearchApi RespondWithRawJson(string indexName, string json)
    {
        _responsesByIndex[indexName] = () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return this;
    }

    /// <summary>Serves an Azure Search error for the given index name.</summary>
    public FakeAzureSearchApi RespondWithError(string indexName, HttpStatusCode statusCode, string message)
    {
        _responsesByIndex[indexName] = () => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                ErrorJson(((int)statusCode).ToString(), message),
                Encoding.UTF8,
                "application/json")
        };

        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        var indexName = IndexNameRegex().Match(path) is { Success: true } match ? match.Groups[1].Value : string.Empty;

        var body = request.Content is null
            ? "{}"
            : await request.Content.ReadAsStringAsync(cancellationToken);

        _requests.Add(new SearchRequest(
            indexName,
            request.Headers.TryGetValues("api-key", out var apiKeys) ? string.Join(",", apiKeys) : null,
            JsonDocument.Parse(body).RootElement.Clone()));

        if (_responsesByIndex.TryGetValue(indexName, out var respond))
            return respond();

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                ErrorJson("IndexNotFound", $"No fake response configured for index '{indexName}'."),
                Encoding.UTF8,
                "application/json")
        };
    }

    // The handler instance outlives the HttpClient that HttpClientFactory builds around it.
    protected override void Dispose(bool disposing) { }

    private static string ErrorJson(string code, string message) =>
        "{\"error\":{\"code\":" + JsonSerializer.Serialize(code) +
        ",\"message\":" + JsonSerializer.Serialize(message) + "}}";

    [GeneratedRegex(@"/indexes\('([^']+)'\)", RegexOptions.IgnoreCase)]
    private static partial Regex IndexNameRegex();

    /// <summary>A document to return from a fake search, with its relevance score.</summary>
    public sealed record SearchHit(double Score, Dictionary<string, object?> Fields);

    /// <summary>A search request captured at the HTTP boundary.</summary>
    public sealed record SearchRequest(string IndexName, string? ApiKey, JsonElement Body)
    {
        public string? Search => Text("search");
        public string? Filter => Text("filter");
        public string? Select => Text("select");
        public string? QueryType => Text("queryType");
        public int? Top => Body.TryGetProperty("top", out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
        public bool? IncludeTotalCount => Body.TryGetProperty("count", out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

        private string? Text(string propertyName) =>
            Body.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}
