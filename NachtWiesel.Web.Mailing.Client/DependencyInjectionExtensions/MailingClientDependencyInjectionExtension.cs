using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NachtWiesel.Web.Mailing.Client.Models;
using NachtWiesel.Web.Mailing.Client.Services;

namespace NachtWiesel.Web.Mailing.Client.DependencyInjectionExtensions;

public static class MailingClientDependencyInjectionExtension
{
    public static IHostApplicationBuilder AddMailingClient(this IHostApplicationBuilder builder)
    {
        ConfigureConnection(builder.Services, builder.Configuration, builder.Environment);
        builder.Services.AddTransient<IMailerCommunicatorService, MailerCommunicatorService>();
        return builder;
    }

    private static void ConfigureConnection(IServiceCollection services, IConfigurationManager configuration, IHostEnvironment environment)
    {
        string finalSectionName = environment.EnvironmentName;
        var finalSection = configuration.GetSection("Mailer").GetChildren().FirstOrDefault(x => x.Key == finalSectionName);
        if (finalSection == null)
        {
            throw new Exception($"Section {finalSectionName} not found inside Mailer section");
        }
        var config = new MailerCommunicatorServiceConfig();
        finalSection.Bind(config);
        services.AddSingleton(config);
    }
}