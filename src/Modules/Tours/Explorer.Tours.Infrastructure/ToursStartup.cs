// src/Modules/Tours/Explorer.Tours.Infrastructure/ToursStartup.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.BuildingBlocks.Infrastructure.Database;
using Explorer.Tours.API.Public.Administration;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.API.Public.Internal;
using Explorer.Tours.Core.Domain;
using Explorer.Tours.Core.Mappers;
using Explorer.Tours.Core.UseCases.Administration;
using Explorer.Tours.Core.UseCases.Tourist;
using Explorer.Tours.Core.UseCases.Internal;
using Explorer.Tours.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http; // Add this for HttpClient extensions

namespace Explorer.Tours.Infrastructure;

public static class ToursStartup
{
    public static IServiceCollection ConfigureToursModule(this IServiceCollection services)
    {
        // Registers all profiles since it works on the assembly
        services.AddAutoMapper(typeof(ToursProfile).Assembly);
        SetupCore(services);
        SetupInfrastructure(services);
        return services;
    }

    private static void SetupCore(IServiceCollection services)
    {
        // Existing services
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<ITourService, TourService>();
        services.AddScoped<ITouristTourService, TouristTourService>();
        services.AddScoped<IKeyPointService, KeyPointService>();

        // New purchase-related services
        services.AddScoped<IShoppingCartService, ShoppingCartService>();
        services.AddScoped<IBonusPointsService, BonusPointsService>();
        services.AddScoped<ITourPurchaseService, TourPurchaseService>();

        // Email service with direct repository access
        services.AddScoped<IEmailService, EmailService>();
    }

    private static void SetupInfrastructure(IServiceCollection services)
    {
        // Existing repositories
        services.AddScoped(typeof(ICrudRepository<Equipment>), typeof(CrudDatabaseRepository<Equipment, ToursContext>));
        services.AddScoped(typeof(ICrudRepository<Tour>), typeof(CrudDatabaseRepository<Tour, ToursContext>));
        services.AddScoped(typeof(ICrudRepository<KeyPoint>), typeof(CrudDatabaseRepository<KeyPoint, ToursContext>));

        // New purchase-related repositories
        services.AddScoped(typeof(ICrudRepository<ShoppingCart>), typeof(CrudDatabaseRepository<ShoppingCart, ToursContext>));
        services.AddScoped(typeof(ICrudRepository<TourPurchase>), typeof(CrudDatabaseRepository<TourPurchase, ToursContext>));
        services.AddScoped(typeof(ICrudRepository<BonusPoints>), typeof(CrudDatabaseRepository<BonusPoints, ToursContext>));
        services.AddScoped(typeof(ICrudRepository<BonusTransaction>), typeof(CrudDatabaseRepository<BonusTransaction, ToursContext>));

        services.AddDbContext<ToursContext>(opt =>
            opt.UseNpgsql(DbConnectionStringBuilder.Build("tours"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", "tours")));
    }
}