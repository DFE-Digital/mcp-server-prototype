using Dfe.Mcp.Server.Application.Services.Interfaces;

namespace Dfe.Mcp.Server.Application.Services;

public sealed class DateTimeService : IDateTimeService
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public int CurrentYear => Now.Year;
}
