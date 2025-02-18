using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using PapyrusClient.Models;

namespace PapyrusClient.Ui.Dialogs;

public partial class FileDetailsDialog : ComponentBase
{
    private enum ActiveTabType : byte
    {
        Status = 0,
        Options = 1,
        Details = 2
    }

    [CascadingParameter] public FluentDialog Dialog { get; set; } = null!;

    [Parameter] public WorkScheduleFile Content { get; set; } = null!;

    private string ActiveTab { get; set; } = GetActiveTabAsString(ActiveTabType.Status);

    private string GetMessage()
    {
        return Content.Status.State switch
        {
            WorkScheduleFileState.Ok => "File has been processed successfully.",
            WorkScheduleFileState.ReadError => "File could not be read.",
            WorkScheduleFileState.ValidateError => "File could not be validated.",
            WorkScheduleFileState.GeneralError => "File could not be processed.",
            _ => "File could not be processed."
        };
    }

    private async Task CloseAsync()
    {
        await Dialog.CloseAsync();
    }

    private static string GetActiveTabAsString(ActiveTabType type)
    {
        return type switch
        {
            ActiveTabType.Status => nameof(ActiveTabType.Status),
            ActiveTabType.Options => nameof(ActiveTabType.Options),
            ActiveTabType.Details => nameof(ActiveTabType.Details),
            _ => "Unknown"
        };
    }
}