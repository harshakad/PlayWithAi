using Microsoft.Extensions.DependencyInjection;
using GamePlatform.Application.Games;
using GamePlatform.Application.Interfaces;

namespace GamePlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IGameService, GameService>();
        return services;
    }
}