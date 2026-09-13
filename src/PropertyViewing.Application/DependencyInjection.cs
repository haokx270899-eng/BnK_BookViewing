using Microsoft.Extensions.DependencyInjection;
using PropertyViewing.Application.Interfaces;
using PropertyViewing.Application.Services;

namespace PropertyViewing.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IViewingService, ViewingService>();

            return services;
        }
    }
}
