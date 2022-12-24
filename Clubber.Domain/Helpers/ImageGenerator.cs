using Clubber.Domain.Models.Responses;
using Serilog;
using System.Diagnostics;
using System.Web;

namespace Clubber.Domain.Helpers;

public class ImageGenerator
{
	private const string _toolFilename = "wkhtmltoimage";
	private readonly string _baseDirectory = AppContext.BaseDirectory;
	private readonly string _toolFilepath;

	public ImageGenerator()
	{
		if (OperatingSystem.IsWindows())
			_toolFilepath = _toolFilename + ".exe";
		else
			_toolFilepath = _toolFilename;
	}

	/// <returns>A MemoryStream of the newly generated image file.</returns>
	public async Task<MemoryStream> FromEntryResponse(EntryResponse entry, string? playerCountryCode, int width = 1100)
	{
		Log.Debug("In FromEntryResponse");

		string baseFlagPath = Path.Combine(AppContext.BaseDirectory, "Data", "Flags");
		string flagPath = Path.Combine(baseFlagPath, $"{playerCountryCode}.png");

		if (string.IsNullOrEmpty(playerCountryCode) || !File.Exists(flagPath))
			flagPath = Path.Combine(baseFlagPath, "00.png");

		if (OperatingSystem.IsWindows())
			flagPath = "file:///" + flagPath;

		Log.Debug("flagPath: {FlagPath}", flagPath);

		string ddinfoStyleHtml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "DdinfoStyle.txt"));
		string formattedHtml = string.Format(
			ddinfoStyleHtml,
			entry.Rank,
			flagPath,
			HttpUtility.HtmlEncode(entry.Username),
			$"{entry.Time / 10000d:0.0000}");

		return await FromHtml(formattedHtml, width);
	}

	/// <returns>A MemoryStream of the newly generated image file.</returns>
	public async Task<MemoryStream> FromHtml(string html, int width = 1100)
	{
		Log.Debug("In FromHtml");
		
		string tempHtmlFileName = Path.Combine(_baseDirectory, "Data", $"{Guid.NewGuid()}.html");

		Log.Debug("HTML file path: {HtmlPath}", tempHtmlFileName);

		await File.WriteAllTextAsync(tempHtmlFileName, html);
		MemoryStream imageStream = await FromFile(tempHtmlFileName, width);
		File.Delete(tempHtmlFileName);

		Log.Debug("Returning memory stream in FromHtml");
		return imageStream;
	}

	private async Task<MemoryStream> FromFile(string htmlFilePath, int width)
	{
		Log.Debug("In FromFile");

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

		Log.Debug("Awaiting process");

		await process.WaitForExitAsync();

		Log.Debug("Process finished");

		if (!File.Exists(newImagePath))
			throw new("Something went wrong. Please check input parameters or that wkhtmltopdf is installed on this machine.");

		Log.Debug("Image file path: {ImagePath}", newImagePath);

		byte[] imageBytes = await File.ReadAllBytesAsync(newImagePath);
		File.Delete(newImagePath);

		Log.Debug("Returning image in FromFile");
		return new(imageBytes);
	}
}
