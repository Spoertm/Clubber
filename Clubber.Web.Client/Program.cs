using Blazored.LocalStorage;
using Clubber.Web.Client;
using Clubber.Web.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<DarkModeManager>();

WebAssemblyHost app = builder.Build();

await app.Services.GetRequiredService<DarkModeManager>().Init();

await app.RunAsync();
