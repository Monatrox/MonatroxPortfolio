using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Contact.Application;
using MonatroxPortfolio.Contact.Application.Abstractions;

namespace MonatroxPortfolio.Contact.Infrastructure;

public static class Registration
{
    public static IServiceCollection AddContact(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddScoped<IContactSender, SmtpContactSender>();
        services.AddScoped<ContactService>();
        services.AddContactRateLimiting();
        return services;
    }
}
