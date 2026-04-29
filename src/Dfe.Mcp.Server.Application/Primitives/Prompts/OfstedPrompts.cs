using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Prompts;

[McpServerPromptType]
public static class OfstedPrompts
{
    [McpServerPrompt(Name = "SummariseRating"), Description("Generates a prompt to summarise an Ofsted rating report for a given school.")]
    public static string SummariseRating(
        [Description("The school name")] string schoolName,
        [Description("Overall effectiveness rating e.g. Outstanding, Good, Requires Improvement, Inadequate")] string overallEffectiveness,
        [Description("Quality of education rating")] string qualityOfEducation,
        [Description("Behaviour and attitudes rating")] string behaviourAndAttitudes,
        [Description("Leadership and management rating")] string leadershipAndManagement,
        [Description("Date of inspection (ISO 8601)")] string inspectionDate)
    {
        return $"""
            You are an education analyst. Summarise the following Ofsted inspection results for {schoolName}.

            Inspection Date: {inspectionDate}

            Ratings:
            - Overall Effectiveness:      {overallEffectiveness}
            - Quality of Education:       {qualityOfEducation}
            - Behaviour and Attitudes:    {behaviourAndAttitudes}
            - Leadership and Management:  {leadershipAndManagement}

            Provide:
            1. A concise summary of the school's performance.
            2. Key strengths based on the ratings.
            3. Areas that may need improvement.
            4. A comparison to national Ofsted standards.

            Keep the tone professional and suitable for a parent or governor audience.
            """;
    }

    [McpServerPrompt(Name = "CompareRatings"), Description("Generates a prompt to compare two schools' Ofsted ratings.")]
    public static string CompareRatings(
        [Description("Name of the first school")] string schoolNameA,
        [Description("Overall effectiveness of the first school")] string overallA,
        [Description("Name of the second school")] string schoolNameB,
        [Description("Overall effectiveness of the second school")] string overallB)
    {
        return $"""
            You are an education analyst. Compare the Ofsted inspection results for the following two schools:

            School A: {schoolNameA}
            - Overall Effectiveness: {overallA}

            School B: {schoolNameB}
            - Overall Effectiveness: {overallB}

            Provide:
            1. A side-by-side comparison of their overall performance.
            2. Which school performed better and why.
            3. Recommendations for both schools based on their ratings.

            Keep the tone neutral and data-driven.
            """;
    }

    [McpServerPrompt, Description("Generates a prompt to recommend schools based on a minimum Ofsted rating threshold.")]
    public static string RecommendSchools(
        [Description("Minimum acceptable rating e.g. Good, Outstanding")] string minimumRating,
        [Description("Local area or borough to focus on")] string area)
    {
        return $"""
            You are an education adviser helping a parent find the best school.

            The parent is looking for schools in {area} with an Ofsted rating of at least '{minimumRating}'.

            Based on available Ofsted data:
            1. List schools that meet or exceed this threshold.
            2. Highlight any Outstanding-rated schools.
            3. Flag any schools rated 'Requires Improvement' or 'Inadequate' to avoid.
            4. Provide a brief recommendation for the top choice.

            Keep the advice clear, concise, and parent-friendly.
            """;
    }
}
