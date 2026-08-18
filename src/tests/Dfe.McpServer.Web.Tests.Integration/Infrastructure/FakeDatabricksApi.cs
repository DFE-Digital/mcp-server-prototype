using System.Net;
using System.Text;
using System.Text.Json;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;

/// <summary>
/// Stands in for the Databricks SQL Statement Execution API at the HTTP transport boundary.
///
/// The real <see cref="Dfe.Mcp.Server.Application.Services.DatabricksSqlService"/> runs against this,
/// so the generated SQL, the submit request, the poll loop and the result extraction are all
/// genuinely exercised.
/// </summary>
public sealed class FakeDatabricksApi : HttpMessageHandler
{
    private const string StatementId = "01ef-fake-statement-id";

    private readonly List<StatementSubmission> _submissions = [];
    private readonly Queue<Func<HttpResponseMessage>> _pollResponses = new();

    private Func<HttpResponseMessage>? _submitResponse;
    private Func<HttpResponseMessage>? _finalPollResponse;

    /// <summary>Every statement the service submitted, in order.</summary>
    public IReadOnlyList<StatementSubmission> Submissions => _submissions;

    /// <summary>The single statement the service submitted. Fails if there was not exactly one.</summary>
    public StatementSubmission SingleSubmission => Assert.Single(_submissions);

    /// <summary>Number of times the service polled for the statement result.</summary>
    public int PollCount { get; private set; }

    /// <summary>Completes the statement immediately with the supplied result payload.</summary>
    public FakeDatabricksApi RespondWithResult(long rowCount, params string[][] dataArrayRows)
    {
        var rows = string.Join(",", dataArrayRows.Select(row =>
            $"[{string.Join(",", row.Select(value => JsonSerializer.Serialize(value)))}]"));

        return RespondWithRawResult($$"""
            {
              "statement_id": "{{StatementId}}",
              "status": { "state": "SUCCEEDED" },
              "result": { "row_count": {{rowCount}}, "data_array": [{{rows}}] }
            }
            """);
    }

    /// <summary>Completes the statement with a verbatim Databricks poll payload.</summary>
    public FakeDatabricksApi RespondWithRawResult(string json)
    {
        _finalPollResponse = () => Json(HttpStatusCode.OK, json);
        return this;
    }

    /// <summary>
    /// Reports the statement as still running for <paramref name="pendingPolls"/> polls before
    /// completing, so the service's poll loop is exercised.
    /// </summary>
    public FakeDatabricksApi RespondWithPendingPolls(int pendingPolls)
    {
        for (var i = 0; i < pendingPolls; i++)
            _pollResponses.Enqueue(() => Json(HttpStatusCode.OK, RunningJson));

        return this;
    }

    /// <summary>Reports the statement as terminally failed.</summary>
    public FakeDatabricksApi RespondWithFailedStatement(string message)
    {
        _finalPollResponse = () => Json(HttpStatusCode.OK, $$"""
            {
              "statement_id": "{{StatementId}}",
              "status": { "state": "FAILED", "error": { "message": {{JsonSerializer.Serialize(message)}} } }
            }
            """);

        return this;
    }

    /// <summary>Rejects the submit call with an HTTP error.</summary>
    public FakeDatabricksApi RespondWithSubmitError(HttpStatusCode statusCode, string body)
    {
        _submitResponse = () => Json(statusCode, body);
        return this;
    }

    /// <summary>Rejects the poll call with an HTTP error.</summary>
    public FakeDatabricksApi RespondWithPollError(HttpStatusCode statusCode, string body)
    {
        _finalPollResponse = () => Json(statusCode, body);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var bearerToken = request.Headers.Authorization?.Parameter;

        if (request.Method == HttpMethod.Post)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var json = JsonDocument.Parse(body).RootElement;

            _submissions.Add(new StatementSubmission(
                json.GetProperty("statement").GetString()!,
                json.GetProperty("warehouse_id").GetString(),
                json.TryGetProperty("wait_timeout", out var waitTimeout) ? waitTimeout.GetString() : null,
                bearerToken,
                request.RequestUri!.AbsolutePath));

            return _submitResponse is not null
                ? _submitResponse()
                : Json(HttpStatusCode.OK, SubmitAcceptedJson);
        }

        PollCount++;

        if (_pollResponses.Count > 0)
            return _pollResponses.Dequeue()();

        return _finalPollResponse is not null
            ? _finalPollResponse()
            : Json(HttpStatusCode.OK, EmptySucceededJson);
    }

    // The handler instance outlives the HttpClient that HttpClientFactory builds around it.
    protected override void Dispose(bool disposing) { }

    private const string SubmitAcceptedJson = """{"statement_id":"01ef-fake-statement-id"}""";

    private const string RunningJson =  """{"statement_id":"01ef-fake-statement-id","status":{"state":"RUNNING"}}""";

    private const string EmptySucceededJson = 
        """{"statement_id":"01ef-fake-statement-id","status":{"state":"SUCCEEDED"},"result":{"row_count":0,"data_array":[]}}""";

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>A statement submission captured at the HTTP boundary.</summary>
    public sealed record StatementSubmission(
        string Statement,
        string? WarehouseId,
        string? WaitTimeout,
        string? BearerToken,
        string Path)
    {
        /// <summary>The statement with runs of whitespace collapsed, for readable assertions.</summary>
        public string NormalisedStatement =>
            string.Join(' ', Statement.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
