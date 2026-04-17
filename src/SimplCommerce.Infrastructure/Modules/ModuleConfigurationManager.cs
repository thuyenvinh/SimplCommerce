using System;
using System.Collections.Generic;

namespace SimplCommerce.Infrastructure.Modules
{
    /// <summary>
    /// Static module manifest. Replaces the legacy <c>modules.json</c> file read at startup.
    ///
    /// Every bundled module is referenced statically from the host (via <c>&lt;ProjectReference&gt;</c>),
    /// so the manifest only has to enumerate which ones should actually be activated in a given
    /// host. Optional variants (payment gateways, email senders, storage backends) can be enabled
    /// or disabled by editing this list — rebuild required.
    ///
    /// The list is kept in the same order as the previous <c>modules.json</c> so module
    /// initializer ordering (which callers occasionally depend on) remains stable.
    /// </summary>
    public class ModuleConfigurationManager : IModuleConfigurationManager
    {
        private static readonly ModuleInfo[] _manifest =
        {
            Bundled("SimplCommerce.Module.ActivityLog"),
            Bundled("SimplCommerce.Module.Catalog"),
            Bundled("SimplCommerce.Module.Cms"),
            Bundled("SimplCommerce.Module.Checkouts"),
            Bundled("SimplCommerce.Module.Comments"),
            Bundled("SimplCommerce.Module.Contacts"),
            Bundled("SimplCommerce.Module.Core"),
            Bundled("SimplCommerce.Module.DinkToPdf"),
            Bundled("SimplCommerce.Module.EmailSenderSmtp"),
            Bundled("SimplCommerce.Module.Inventory"),
            Bundled("SimplCommerce.Module.Localization"),
            Bundled("SimplCommerce.Module.News"),
            Bundled("SimplCommerce.Module.Orders"),
            Bundled("SimplCommerce.Module.PaymentBraintree"),
            Bundled("SimplCommerce.Module.PaymentCoD"),
            Bundled("SimplCommerce.Module.PaymentPaypalExpress"),
            Bundled("SimplCommerce.Module.Payments"),
            Bundled("SimplCommerce.Module.PaymentStripe"),
            Bundled("SimplCommerce.Module.PaymentMomo"),
            Bundled("SimplCommerce.Module.PaymentNganLuong"),
            Bundled("SimplCommerce.Module.PaymentCashfree"),
            Bundled("SimplCommerce.Module.Pricing"),
            Bundled("SimplCommerce.Module.ProductComparison"),
            Bundled("SimplCommerce.Module.ProductRecentlyViewed"),
            Bundled("SimplCommerce.Module.Reviews"),
            Bundled("SimplCommerce.Module.SampleData"),
            Bundled("SimplCommerce.Module.Search"),
            Bundled("SimplCommerce.Module.Shipments"),
            Bundled("SimplCommerce.Module.Shipping"),
            Bundled("SimplCommerce.Module.ShippingFree"),
            Bundled("SimplCommerce.Module.ShippingPrices"),
            Bundled("SimplCommerce.Module.ShippingTableRate"),
            Bundled("SimplCommerce.Module.ShoppingCart"),
            Bundled("SimplCommerce.Module.StorageLocal"),
            Bundled("SimplCommerce.Module.Tax"),
            Bundled("SimplCommerce.Module.Vendors"),
            Bundled("SimplCommerce.Module.WishList"),
        };

        public IEnumerable<ModuleInfo> GetModules() => _manifest;

        private static ModuleInfo Bundled(string id) => new()
        {
            Id = id,
            Version = new Version(1, 0, 0),
            IsBundledWithHost = true,
        };
    }
}
