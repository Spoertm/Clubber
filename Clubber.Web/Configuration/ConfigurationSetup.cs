using Clubber.Domain.Configuration;
using System.Text;

namespace Clubber.Web.Configuration;

internal static class ConfigurationSetup
{
	public static void ConfigureConfiguration(this WebApplicationBuilder builder)
	{
		string appSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
		if (File.Exists(appSettingsPath))
		{
			if (builder.Environment.EnvironmentName == "Development")
			{
				builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
			}
			else
			{
				builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
			}
		}
		else
		{
			string configJson = Environment.GetEnvironmentVariable("Configuration") ?? throw new InvalidOperationException("Configuration environment variable not set");

			using MemoryStream stream = new(Encoding.UTF8.GetBytes(configJson));
			builder.Configuration.AddJsonStream(stream);
		}

		builder.Services.AddOptions<AppConfig>()
			.Bind(builder.Configuration)
			.ValidateDataAnnotations()
			.ValidateOnStart();
	}
}
