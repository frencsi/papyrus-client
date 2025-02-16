using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PapyrusClient.Services.SettingsManager;

namespace PapyrusClient.Utilities;

public static class BlazorExtensions
{
    /// <inheritdoc cref="WebAssemblyHost.RunAsync"/>
    public static async Task BuildAndRunAsync(this WebAssemblyHostBuilder builder)
    {
        var host = builder.Build();

        var settingsManager = host.Services.GetRequiredService<ISettingsManager>();

        await settingsManager.LoadSettingsAsync();

        await host.RunAsync();
    }
}