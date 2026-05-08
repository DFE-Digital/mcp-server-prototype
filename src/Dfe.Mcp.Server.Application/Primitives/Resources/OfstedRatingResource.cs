using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Domain;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Dfe.Mcp.Server.Application.Primitives.Resources;

[McpServerResourceType]
public class OfstedRatingResource
{
    private readonly List<OfstedRatingModel> _ratings =
    [
        new OfstedRatingModel
        {
            Id = 1,
            SchoolName = "Greenwood Academy",
            OverallEffectiveness = "Outstanding",
            QualityOfEducation = "Good",
            BehaviourAndAttitudes = "Good",
            LeadershipAndManagement = "Outstanding",
            InspectionDate = new DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc)
        },
        new OfstedRatingModel
        {
            Id = 2,
            SchoolName = "Riverside High",
            OverallEffectiveness = "Good",
            QualityOfEducation = "Good",
            BehaviourAndAttitudes = "Good",
            LeadershipAndManagement = "Good",
            InspectionDate = new DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    [McpServerResource(UriTemplate = "ofsted://ratings", Title = "All Ofsted ratings", Name = "GetAllOfstedRatings", MimeType = "application/json"), Description("Returns all Ofsted ratings as JSON.")]
    [Authorize(Policy = Policy.ResourceAccess)]
    public Task<string> GetAllOfstedRatingsAsync()
    {
        return Task.FromResult(JsonSerializer.Serialize(_ratings));
    }

    [McpServerResource(UriTemplate = "ofsted://ratings/{id}", Title = "Get Ofsted ratings by id", Name = "GetOfstedRatingById", MimeType = "application/json"), Description("Returns a single Ofsted rating by ID.")]
    [Authorize(Policy = Policy.ResourceAccess)]
    public Task<string> GetOfstedRatingByIdAsync(
        [Description("The rating ID")] int id)
    {
        var rating = _ratings.FirstOrDefault(r => r.Id == id);

        return rating is null
            ? Task.FromResult($"No rating found with ID {id}.")
            : Task.FromResult(JsonSerializer.Serialize(rating));
    }
}
