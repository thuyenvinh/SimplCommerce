using Xunit;

namespace SimplCommerce.ApiService.IntegrationTests;

/// <summary>
/// Always-green smoke test that runs without Docker, so CI jobs that can't afford
/// Testcontainers (or that hit a transient Docker Hub outage) still prove the test
/// assembly compiles and xunit discovers it. The real behavioural tests live under
/// the <c>RequiresDocker</c> trait in the other classes in this project.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Scaffold_Compiles_And_xunit_Discovers() => Assert.True(true);
}
