using Microsoft.Extensions.DependencyInjection;

namespace SimplCommerce.Module.PaymentVnpay;

public static class PaymentVnpayModuleExtensions
{
    public static IServiceCollection AddPaymentVnpayModule(this IServiceCollection services)
    {
        // Pure-static module — no DI to register. The endpoints are mapped from the host.
        return services;
    }
}
