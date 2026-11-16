using FixNear.Features.Auth.Configurations;
using FixNear.Features.Auth.Interfaces;
using FixNear.Features.Auth.Services;

using FixNear.Features.Customers.Interfaces;
using FixNear.Features.Customers.Services;

using FixNear.Features.Invoices.Interfaces;
using FixNear.Features.Invoices.Services;

using FixNear.Features.Manager.Interfaces;
using FixNear.Features.Manager.Services;

using FixNear.Features.Payments.Interfaces;
using FixNear.Features.Payments.Services;

using FixNear.Features.Quotes.Interfaces;
using FixNear.Features.Quotes.Services;

using FixNear.Features.RepairRequest.Interfaces;
using FixNear.Features.RepairRequest.Services;

using FixNear.Features.Reviews.Interfaces;
using FixNear.Features.Reviews.Services;

using FixNear.Features.ServiceCatalog.Interfaces;
using FixNear.Features.ServiceCatalog.Services;

using FixNear.Features.Technicians.Interfaces;
using FixNear.Features.Technicians.Services;

using FixNear.Infrastructure.Persistence;
using FixNear.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Infrastructure.DependencyInjection;

public static class ServicesRegistration
{
    public static IServiceCollection AddFixNearServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FixNearDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Authentication
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Customer & Manager
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IManagerService, ManagerService>();

        // Service Catalog
        services.AddScoped<IServiceCategoryService, ServiceCategoryService>();

        // Repair Request
        services.AddScoped<IRepairItemService, RepairItemService>();

        // Technician
        services.AddScoped<ITechnicianService, TechnicianService>();

        // Quote
        services.AddScoped<IQuoteService, QuoteService>();

        // Invoice & Payment
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Review
        services.AddScoped<IReviewService, ReviewService>();

        return services;
    }
}
