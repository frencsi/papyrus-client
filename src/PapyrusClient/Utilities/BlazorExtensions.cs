using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace PapyrusClient.Utilities;

public static class BlazorExtensions
{
    /// <inheritdoc cref="WebAssemblyHost.RunAsync"/>
    public static async Task BuildAndRunAsync(this WebAssemblyHostBuilder builder)
    {
        var host = builder.Build();

        var clientManager = host.Services.GetRequiredService<IClientManager>();
        await clientManager.InitializeAsync(reload: false);

        var workScheduleStore = host.Services.GetRequiredService<IWorkScheduleStore>();
        await workScheduleStore.InitializeAsync();

        await host.RunAsync();
    }
}