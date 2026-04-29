using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

namespace Dfe.Mcp.Server.Application.Primitives.Tools;

[McpServerToolType]
public class ElicitationTools
{
    [McpServerTool(Name = "get_current_datetime")]
    [Description("Provides the current date and time in UTC format")]
    public async Task<string> GetCurrentDateTime2(McpServer mcpServer)
    {
        if (mcpServer.ClientCapabilities?.Elicitation == null)
        {
            throw new McpException("Client does not support Elicitation!");
        }

        var elicitationResult = await mcpServer.ElicitAsync(GetApprovalParams(nameof(GetCurrentDateTime2)));
        if (elicitationResult.Action != "accept" ||
          elicitationResult.Content?["answer"].ValueKind != JsonValueKind.True)
        {
            throw new McpException("User declined to proceed");
        }

        return DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
    }

    private static ElicitRequestParams GetApprovalParams(string name) => new()
    {
        Message = $"Do you want to execute tool '{name}'",
        RequestedSchema = new RequestSchema()
        {
            Properties =
            {
                ["answer"] = new BooleanSchema()
            },
        },
    };
}
