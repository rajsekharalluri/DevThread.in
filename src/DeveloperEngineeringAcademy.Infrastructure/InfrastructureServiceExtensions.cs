using DeveloperEngineeringAcademy.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DeveloperEngineeringAcademy.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddContentInfrastructure(this IServiceCollection services, string contentRoot)
    {
        services.AddSingleton<IContentRepository>(_ => new FileSystemContentRepository(contentRoot));
        services.AddSingleton<ISearchService, InMemorySearchService>();
        services.AddSingleton<IContentValidator, ContentValidator>();
        services.AddSingleton<CategoryService>();
        return services;
    }
}
