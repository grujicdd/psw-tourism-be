using Explorer.Blog.Infrastructure;
using Explorer.Stakeholders.Infrastructure;
using Explorer.Tours.Infrastructure;

namespace Explorer.API.Startup;

public static class ModulesConfiguration
{
    public static IServiceCollection RegisterModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureStakeholdersModule();
        services.ConfigureToursModule(configuration);  // Pass configuration here
        services.ConfigureBlogModule();

        return services;
    }
}