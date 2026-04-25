using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.PaymentNganLuong
{
    public static class PaymentNganLuongModuleExtensions
    {
        public static IServiceCollection AddPaymentNganLuongModule(this IServiceCollection services)
        {
            GlobalConfiguration.RegisterAngularModule("simplAdmin.paymentNganLuong");
            return services;
        }
    }
}
