using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure;

namespace SimplCommerce.Module.Contacts
{
    public static class ContactsModuleExtensions
    {
        public static IServiceCollection AddContactsModule(this IServiceCollection services)
        {
            return services;
        }
    }
}
