using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Enums;

public enum PromptType
{
    [Description("You are a tool that helps civil servants at the UK Department for Education write reports, briefs and submissions to regional directors")]
    SystemInstruction,
    [Description("You are analysing Ofsted inspection data with structured reasoning.")]
    Ofsted,
    [Description("You are handling safeguarding and concern-related information responsibly.")]
    Concern
}
