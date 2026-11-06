using GigHaus.Infrastructure.Data;
using GigHaus.Infrastructure.Email;
using GigHaus.Infrastructure.Messaging;
using GigHaus.Infrastructure.Repositories;
using GigHaus.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GigHaus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGigHausInfrastructure(this IServiceCollection services)
    {
        // Data access
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IHostRepository, HostRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IPaymentRecordRepository, PaymentRecordRepository>();
        services.AddScoped<ISettlementSummaryRepository, SettlementSummaryRepository>();

        // Services
        services.AddScoped<IReservationService, ReservationService>();

        return services;
    }

    public static IServiceCollection AddGigHausPayments(this IServiceCollection services, Action<StripeOptions>? configure = null)
    {
        var options = new StripeOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddScoped<IPaymentService, StripePaymentService>();
        return services;
    }

    public static IServiceCollection AddGigHausMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        services.AddHostedService<EmailNotificationWorker>();
        return services;
    }

    public static IServiceCollection AddGigHausEmail(this IServiceCollection services, Action<SmtpEmailOptions>? configure = null)
    {
        var options = new SmtpEmailOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IEmailService, SmtpEmailService>();
        return services;
    }
}

