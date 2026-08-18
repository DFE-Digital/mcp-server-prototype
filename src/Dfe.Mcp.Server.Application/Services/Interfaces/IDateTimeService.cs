namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface IDateTimeService
{
    DateTimeOffset Now { get; }

    int CurrentYear { get; }
}
