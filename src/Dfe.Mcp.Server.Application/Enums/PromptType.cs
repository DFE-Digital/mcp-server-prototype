using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Enums;

public enum SystemPromptType
{
    [Description("You are a tool that helps civil servants at the UK Department for Education write reports, briefs and submissions to regional directors")]
    BriefingTool,
    
    OfstedSummary
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
    OverallSummary,
    [Description("Instructs the LLM to summarise Ofsted inspection results for a school or trust into a plain-English overview covering the most recent 3 years of available data. Output format depends on whether Ofsted information was also separately requested: an H3 sub-section if so, or a standalone H2 section with up to 4 paragraphs if not.")]
    OfstedSummaryTemplate
}
