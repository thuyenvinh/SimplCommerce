using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace SimplCommerce.Module.Core.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IConfiguration _config;

        public CurrencyService(IConfiguration config)
        {
            _config = config;
            var currencyCulture = _config.GetValue<string>("Global.CurrencyCulture");
            // Fall back to en-US when the setting is absent (e.g. a host without the
            // full appsettings such as the integration-test host) so the service
            // doesn't throw ArgumentNullException from new CultureInfo(null).
            CurrencyCulture = string.IsNullOrWhiteSpace(currencyCulture)
                ? new CultureInfo("en-US")
                : new CultureInfo(currencyCulture);
        }

        public CultureInfo CurrencyCulture { get; }

        public string FormatCurrency(decimal value)
        {
            var decimalPlace = _config.GetValue<int>("Global.CurrencyDecimalPlace");
            return value.ToString($"C{decimalPlace}", CurrencyCulture);
        }
    }
}
