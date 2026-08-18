using Dfe.Mcp.Server.Application.Helpers;
using Dfe.Mcp.Server.Application.Services;
using Dfe.Mcp.Server.Data;
using Dfe.Mcp.Server.Data.Models;
using Dfe.Mcp.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit; 

namespace Dfe.Mcp.Server.Application.Tests.Services;

public class AcademiesQueryServiceTests
{
    [Fact]
    public async Task RunQueryAsync_ReturnsFilteredEstablishmentsAndCount()
    {
        // Arrange
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<AcademiesDbContext>().UseSqlite(connection) .Options;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        await using (var context = new AcademiesDbContext(options, configuration))
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            context.Establishments.AddRange(
                new MisEstablishment { Urn = 1001, SchoolName = "Alpha Academy", LocalAuthority = "Somerset", Postcode = "TA1 1AA", TotalNumberOfPupils = 120, SafeguardingIsEffective = "Yes" },
                new MisEstablishment { Urn = 1002, SchoolName = "Beta Academy", LocalAuthority = "Somerset", Postcode = "TA1 2BB", TotalNumberOfPupils = 150, SafeguardingIsEffective = "Yes" },
                new MisEstablishment { Urn = 1003, SchoolName = "Gamma Academy", LocalAuthority = "Devon", Postcode = "EX1 1AA", TotalNumberOfPupils = 90, SafeguardingIsEffective = "No" });

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var service = new AcademiesQueryService(NullLogger<AcademiesQueryService>.Instance, new TestDbContextFactory(options, configuration));

        // Action
        var result = await service.RunQueryAsync(new EstablishmentQueryModel
        {
            LocalAuthority = "Somerset",
            MinPupils = 100,
            Limit = 10
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Null(result.Error);

        var establishments = JsonHelper.Deserialize<List<MisEstablishment>>(result.Results!);
        Assert.NotNull(establishments);
        Assert.Equal(2, establishments!.Count);
        Assert.All(establishments, establishment =>
        {
            Assert.Equal("Somerset", establishment.LocalAuthority);
            Assert.True(establishment.TotalNumberOfPupils >= 100);
        });
    }

    private sealed class TestDbContextFactory(DbContextOptions<AcademiesDbContext> options, IConfiguration configuration) : IDbContextFactory<AcademiesDbContext>
    {
        public AcademiesDbContext CreateDbContext()
        {
            return new AcademiesDbContext(options, configuration);
        }
    }
}
