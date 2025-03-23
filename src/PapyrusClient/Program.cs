global using System.Globalization;
global using Microsoft.Extensions.Localization;
global using PapyrusClient.Models;
global using PapyrusClient.Utilities;
global using PapyrusClient.Services.ClientManager;
global using PapyrusClient.Services.WorkScheduleStore;
global using PapyrusClient.Services.WorkScheduleReader;
global using PapyrusClient.Services.WorkScheduleValidator;
global using PapyrusClient.Services.TimeSheetWriter;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Ui;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddLocalization(options => options.ResourcesPath = "Resources")
    .AddSingleton<TimeProvider>(_ => TimeProvider.System)
    .AddSingleton<IClientManager, ClientManager>()
    .AddSingleton<IWorkScheduleStore, InMemoryWorkScheduleStore>()
    .AddSingleton<IWorkScheduleReader, ExcelWorkScheduleReader>()
    .AddSingleton<IWorkScheduleValidator, WorkScheduleValidator>()
    .AddSingleton<ITimeSheetWriter, ExcelTimeSheetWriter>();

builder.Services
    .AddHttpClient<ClientManager>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services
    .AddFluentUIComponents();

await builder.BuildAndRunAsync();