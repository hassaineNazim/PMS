using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pms.Application.Features.Auth;
using Pms.Application.Features.Billing;
using Pms.Application.Features.CashRegister;
using Pms.Application.Features.Charges;
using Pms.Application.Features.CheckIn;
using Pms.Application.Features.Guests;
using Pms.Application.Features.Housekeeping;
using Pms.Application.Features.Invoices;
using Pms.Application.Features.Payments;
using Pms.Application.Features.Pricing;
using Pms.Application.Features.Rates;
using Pms.Application.Features.Reports;
using Pms.Application.Features.Reservations;
using Pms.Application.Features.Rooms;
using Pms.Application.Features.Settings;
using Pms.Application.Features.Staff;
using Pms.Application.Features.Stats;

namespace Pms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IGuestService, GuestService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ICheckInService, CheckInService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IStatsService, StatsService>();

        // New commercial modules
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IChargeService, ChargeService>();
        services.AddScoped<IFolioService, FolioService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<IHousekeepingService, HousekeepingService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
