using Clubber.Models.Responses;
using Clubber.Services;
using System.Diagnostics;
using System.Web;

namespace Clubber.Helpers
{
	public class ImageGenerator
	{
		private const string _toolFilename = "wkhtmltoimage";
		private readonly string _baseDirectory = AppContext.BaseDirectory;
		private readonly string _toolFilepath;
		private readonly IWebService _webService;

		public ImageGenerator(IWebService webService)
		{
			_webService = webService;
			if (OperatingSystem.IsWindows())
				_toolFilepath = "C:\\Program Files\\wkhtmltopdf\\bin\\wkhtmltoimage.exe";
			else if (OperatingSystem.IsLinux())
				_toolFilepath = _toolFilename;
			else
				throw new NotSupportedException("OSX not supported");
		}

		/// <returns>A MemoryStream of the newly generated image file.</returns>
		public async Task<MemoryStream> FromEntryResponse(EntryResponse entry, int width = 1100)
		{
			string countryCode = await _webService.GetCountryCodeForplayer(entry.Id);
			string baseFlagPath = Path.Combine(AppContext.BaseDirectory, "Data", "Flags");
			string flagPath = Path.Combine(baseFlagPath, $"{countryCode}.png");

			if (countryCode.Length == 0 || !File.Exists(flagPath))
				flagPath = Path.Combine(baseFlagPath, "00.png");

			if (OperatingSystem.IsWindows())
				flagPath = "file:///" + flagPath;

			string ddinfoStyleHtml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "DdinfoStyle.txt"));
			string formattedHtml = string.Format(
				ddinfoStyleHtml,
				entry.Rank,
				flagPath,
				HttpUtility.HtmlEncode(entry.Username),
				$"{entry.Time / 10000d:0.0000}");

			return await FromHtml(formattedHtml);
		}

		/// <returns>A MemoryStream of the newly generated image file.</returns>
		public async Task<MemoryStream> FromHtml(string html, int width = 1100)
		{
			string tempHtmlFileName = Path.Combine(_baseDirectory, "Data", $"{Guid.NewGuid()}.html");
			await File.WriteAllTextAsync(tempHtmlFileName, html);
			MemoryStream imageStream = await FromFile(tempHtmlFileName, width);
			File.Delete(tempHtmlFileName);
			return imageStream;
		}

		private async Task<MemoryStream> FromFile(string htmlFilePath, int width)
		{
			string newImagePath = Path.Combine(_baseDirectory, $"{Guid.NewGuid()}.png");
			string args = $"--enable-local-file-access --quality 100 --encoding utf-8 --width {width} -f png {htmlFilePath} {newImagePath}";

			Process process = Process.Start(new ProcessStartInfo(_toolFilepath, args)
			{
				WindowStyle = ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
				UseShellExecute = false,
				WorkingDirectory = _baseDirectory,
				RedirectStandardError = true,
			})!;

			process.ErrorDataReceived += (_, e) => throw new(e.Data);
			await process.WaitForExitAsync();

			if (!File.Exists(newImagePath))
				throw new("Something went wrong. Please check input parameters or that wkhtmltopdf is installed on this machine.");

			byte[] imageBytes = await File.ReadAllBytesAsync(newImagePath);
			File.Delete(newImagePath);
			return new(imageBytes);
		}
	}
}
