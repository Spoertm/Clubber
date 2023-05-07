using Blazored.LocalStorage;

namespace Clubber.Web.Client.Services;

public sealed class DarkModeManager
{
	private readonly IServiceScopeFactory _scopeFactory;
	public event DarkModeToggleHandler? DarkModeToggle;
	public delegate void DarkModeToggleHandler();

	public bool DarkMode { get; private set; }

	public DarkModeManager(IServiceScopeFactory scopeFactory)
	{
		_scopeFactory = scopeFactory;
	}

	public async Task Init()
	{
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		DarkMode = await scope.ServiceProvider.GetRequiredService<ILocalStorageService>().GetItemAsync<bool>("darkmode");
		DarkModeToggle?.Invoke();
	}

	public async Task ToggleDarkMode()
	{
		DarkMode = !DarkMode;
		DarkModeToggle?.Invoke();
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		await scope.ServiceProvider.GetRequiredService<ILocalStorageService>().SetItemAsync("darkmode", DarkMode);
	}
}
