using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Enums;

public enum SystemPromptType
{
    [Description("You are a tool that helps civil servants at the UK Department for Education write reports, briefs and submissions to regional directors")]
    BriefingTool
}

public enum UserPromptType
{
    [Description("You are analysing Ofsted inspection data with structured reasoning.")]
    Ofsted,
    [Description("You are handling safeguarding and concern-related information responsibly.")]
    Concerns,
    [Description("You are summerising Ofsted summary.")]
    OfstedSummary,
    [Description("You are summerising overall summary.")]
    OverallSummary
}
