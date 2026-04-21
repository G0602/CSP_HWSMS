using Xunit;

using HSMS.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HSMS.Tests;

public class DatabaseIntegrationFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        string? connectionString = Environment.GetEnvironmentVariable("HSMS_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", connectionString)
            ])
            .Build();

        string configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("HSMS_TEST_CONNECTION_STRING is missing.");

        await DatabaseInitializer.InitializeAsync(configuredConnectionString);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

[CollectionDefinition("DatabaseIntegration", DisableParallelization = true)]
public class DatabaseIntegrationCollection : ICollectionFixture<DatabaseIntegrationFixture>
{
}
