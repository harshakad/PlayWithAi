using GamePlatform.Infrastructure.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace GamePlatform.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddHostedService<ChessAgent>();
            return services;
        }
    }
}
