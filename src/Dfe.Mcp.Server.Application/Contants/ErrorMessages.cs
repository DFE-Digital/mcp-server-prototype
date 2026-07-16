namespace Dfe.Mcp.Server.Application.Contants;

public static class InfrastructureConfiguration
{
    public const string CorsPolicyName = "ConfiguredCorsPolicy"; 
}

public static class ErrorMessages
{
    public const string InvalidKeyMessage = "Invalid or missing API key";
    public const string OfstedInteractiveInputNotSupported = "Your client does not support interactive input. Please call 'search_ofsted' directly with your query, top, filter, and select parameters.";
    public const string OfstedSamplingNotSupported = "Your client does not support LLM sampling. Please call 'search_ofsted' directly to retrieve raw results.";
    public const string EstablishmentInteractiveInputNotSupported = "Your client does not support interactive input. Please call 'search_establishment' directly with your query, top, filter, and select parameters.";
    public const string CancelledSearch = "Search cancelled.";
    public const string InvalidSearchParameters = "Please provide your valid search parameters.";
}
