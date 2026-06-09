using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pms.Application.Common;
using Pms.Application.Integrations;
using Pms.Infrastructure.Integrations.Display;
using Pms.Infrastructure.Integrations.Notifications;
using Pms.Infrastructure.Integrations.Pdf;
using Pms.Infrastructure.MultiTenancy;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Security;

namespace Pms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=pms;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Multi-tenancy: register the concrete holder once, expose it via the interface.
        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());

        // Security
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Invoicing & documents
        services.AddSingleton<IInvoiceDocumentGenerator, QuestPdfInvoiceGenerator>();
        services.AddSingleton<IPoliceFormGenerator, QuestPdfPoliceFormGenerator>();

        // Guest notifications (SMS/email abstraction; logs by default)
        services.AddSingleton<INotificationProvider, LogNotificationProvider>();

        // In-room display / IPTV (LG by default, no-op otherwise) — chosen by config.
        services.Configure<DisplayOptions>(config.GetSection(DisplayOptions.SectionName));
        var displayProvider = config.GetSection(DisplayOptions.SectionName)["Provider"]?.ToLowerInvariant();
        if (displayProvider == "lg")
        {
            services.AddHttpClient<IDisplayProvider, LgProCentricDisplayProvider>(c =>
                c.Timeout = TimeSpan.FromSeconds(5));
        }
        else
        {
            services.AddScoped<IDisplayProvider, NullDisplayProvider>();
        }

        services.AddScoped<DbInitializer>();

        return services;
    }
}
