namespace SimplCommerce.E2ETests.TestData;

/// <summary>
/// Deterministic-but-unique test data. Slug + email collide easily on the
/// SimplCommerce unique constraints, so every record gets a timestamped
/// suffix scoped to the test run.
/// </summary>
public static class TestDataFactory
{
    private static readonly string Suffix = DateTimeOffset.UtcNow.ToString("yyMMddHHmmss");
    private static int _counter;

    private static string NextToken() => $"{Suffix}-{Interlocked.Increment(ref _counter):D3}";

    public static (string Name, string Slug, decimal Price) NewProduct(string label = "qa")
    {
        var token = NextToken();
        return ($"E2E {label} {token}", $"e2e-{label}-{token}", 99.99m);
    }

    public static (string Name, string Slug) NewBrand()
    {
        var token = NextToken();
        return ($"E2E Brand {token}", $"e2e-brand-{token}");
    }

    public static (string Name, string Slug) NewCategory()
    {
        var token = NextToken();
        return ($"E2E Cat {token}", $"e2e-cat-{token}");
    }

    public static (string Email, string Password) NewCustomer()
    {
        var token = NextToken();
        return ($"e2e.customer.{token}@example.com", "Aa1!Aa1!");
    }
}
