using Xunit;

namespace SimplCommerce.ApiService.IntegrationTests;

/// <summary>
/// Scaffold placeholder. The real smoke tests (see README.md) require SQL Server +
/// Redis + Azure Blob emulator running — those are owned by Aspire's AppHost and
/// need Docker. Until that environment is provisioned, this class exists only so
/// the test project compiles and xunit discovers at least one trivially-passing
/// test — failure here means the compile itself broke, not a behavioural regression.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Scaffold_Compiles_And_xunit_Discovers()
    {
        // Deliberately trivial. Replace with WebApplicationFactory<Program>-driven
        // tests per the runbook in README.md once the ApiService can boot in CI.
        Assert.True(true);
    }
}
