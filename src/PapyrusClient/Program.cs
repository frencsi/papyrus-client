using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Services.SettingsManager;
using PapyrusClient.Services.TimeSheetWriter;
using PapyrusClient.Services.WorkScheduleReader;
using PapyrusClient.Services.WorkScheduleValidator;
using PapyrusClient.Ui;
using PapyrusClient.Utilities;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddLocalization(options => options.ResourcesPath = "Resources")
    .AddSingleton<IWorkScheduleReader, WorkScheduleExcelReader>()
    .AddSingleton<IWorkScheduleValidator, WorkScheduleValidator>()
    .AddSingleton<ITimeSheetWriter, TimeSheetExcelWriter>()
    .AddSingleton<ISettingsManager, SettingsManager>()
    .AddFluentUIComponents();

await builder.BuildAndRunAsync();