using Clubber.Domain.Configuration;
using System.Text;

namespace Clubber.Web.Configuration;

internal static class ConfigurationSetup
{
	public static void ConfigureConfiguration(this WebApplicationBuilder builder)
	{
		if (builder.Environment.IsDevelopment())
		{
			// Development: use file-based config
			builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
		}
		else
		{
			// Production: use environment variable
			string configJson = Environment.GetEnvironmentVariable("Configuration")
			                    ?? throw new InvalidOperationException("Configuration environment variable not set");

			using MemoryStream stream = new(Encoding.UTF8.GetBytes(configJson));
			builder.Configuration.AddJsonStream(stream);
		}

		builder.Services.AddOptions<AppConfig>()
			.Bind(builder.Configuration)
			.ValidateDataAnnotations()
			.ValidateOnStart();
	}
}
