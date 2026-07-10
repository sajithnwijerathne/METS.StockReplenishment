using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<StockReplenishmentDbContext>(options =>
            options.UseInMemoryDatabase("StockReplenishmentDb"));

        services.AddScoped<IReplenishmentRequestRepository, ReplenishmentRequestRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();

        return services;
    }
}