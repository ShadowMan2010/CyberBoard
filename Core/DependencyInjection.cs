using CyberBoard.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberBoard.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCyberBoardCore(this IServiceCollection services)
    {
        services.AddSingleton<DocumentManager>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<FileService>();
        services.AddTransient<ToolService>();
        services.AddTransient<RenderingService>();
        services.AddSingleton<ImportService>();
        return services;
    }
}
