using Microsoft.Extensions.DependencyInjection;
using GamePlatform.Application.Games.Chess;
using GamePlatform.Application.Games.Checkers;

namespace GamePlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IChessGameService, ChessGameService>();
        services.AddScoped<ICheckersGameService, CheckersGameService>();
        return services;
    }
}
