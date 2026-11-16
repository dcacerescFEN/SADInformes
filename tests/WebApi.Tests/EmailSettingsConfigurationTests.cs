using Application.Common.Settings;
using Domain.Entities.AppConfig;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace WebApi.Tests;

[Collection("EmailDispatch")]
public sealed class EmailSettingsConfigurationTests
{
    [Fact]
    public async Task AppConfig_email_keys_override_the_sad_alumnos_defaults()
    {
        var options = new DbContextOptionsBuilder<AppConfigContext>()
            .UseInMemoryDatabase("email-config-" + Guid.NewGuid()).Options;
        await using var context = new AppConfigContext(options);
        var application = new Domain.Entities.AppConfig.Application { ApplicationName = "SADInformes", IsActive = true };
        var environment = new Domain.Entities.AppConfig.Environment { EnvironmentName = "Testing", IsActive = true };
        context.Applications.Add(application);
        context.Environments.Add(environment);
        await context.SaveChangesAsync();

        void Add(string name, string value) => context.Configurations.Add(new Configuration
        {
            ConfigurationName = name,
            ConfigurationValue = value,
            ApplicationId = application.ApplicationId,
            EnvironmentId = environment.EnvironmentId,
            IsActive = true,
        });

        Add("SendEmail", "1");
        Add("EmailFrom", "appconfig-from@fen.uchile.cl");
        Add("EmailTo", "appconfig-to@fen.uchile.cl");
        Add("EmailServer", "smtp.appconfig.cl");
        Add("EmailPort", "2525");
        Add("ReportAvailabilityEmailBcc", "bcc@fen.uchile.cl");
        await context.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConfigureSite:ApplicationName"] = "SADInformes",
            ["ConfigureSite:EnvironmentName"] = "Testing",
        }).Build();
        var initialiser = new ConfigurationInitialiser(
            context, new SiteSettings(), new PostgraduateSettings(), new StudentPhotoSettings(),
            NullLogger<ConfigurationInitialiser>.Instance, new ReportingSettings());

        await initialiser.InitialiseAsync(configuration);

        Assert.True(EmailSettings.SendEmail);
        Assert.Equal("appconfig-from@fen.uchile.cl", EmailSettings.EmailFrom);
        Assert.Equal("appconfig-to@fen.uchile.cl", EmailSettings.EmailTo);
        Assert.Equal("smtp.appconfig.cl", EmailSettings.Server);
        Assert.Equal(2525, EmailSettings.Port);
        Assert.Equal("bcc@fen.uchile.cl", TeacherAvailabilityEmailSettings.Bcc);
    }
}
