using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using PapyrusClient.Models;
using PapyrusClient.Services.SettingsManager;
using PapyrusClient.Ui.Dialogs;
using ResourceKey = PapyrusClient.Resources.Ui.Pages.Home;

namespace PapyrusClient.Ui.Pages;

public partial class Home : ComponentBase, IAsyncDisposable
{
    private enum State : byte
    {
        Idle = 0,
        Loading = 1,
        Processing = 2
    }

    #region Constants

    private const int MaximumFileCountPerLoad = 512;

    #endregion

    #region Fields

    private volatile bool _disposed = false;

    private static readonly List<WorkScheduleFile> WorkScheduleFiles = new(MaximumFileCountPerLoad * 2);

    private IEnumerable<WorkScheduleFile> _selectedWorkScheduleFiles = Array.Empty<WorkScheduleFile>();

    private State _state = State.Idle;

    private int _loadProgressPercent = 0;

    private string _loadFileName = string.Empty;

    private IDialogReference? _dialog;

    #endregion

    protected override void OnInitialized()
    {
        base.OnInitialized();

        SettingsManager.CultureChanged += OnCultureChanged;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        SettingsManager.CultureChanged -= OnCultureChanged;

        WorkScheduleFiles.Clear();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnFileCountExceeded(int count)
    {
        ToastService.ShowWarning(Localizer.GetString(nameof(ResourceKey.InputFileMaximumFilesExceeded),
            MaximumFileCountPerLoad));
    }

    private async Task OnFileLoadedAsync(FluentInputFileEventArgs args)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        Debug.Assert(args.Stream != null, "Stream should not be null");

        _state = State.Loading;

        _loadProgressPercent = args.ProgressPercent;

        _loadFileName = args.Name;

        var fileStream = args.Stream;

        var fileName = args.Name;
        var fileSizeInBytes = args.Size;
        WorkSchedule? workSchedule;
        Exception? exception;

        try
        {
            workSchedule = await WorkScheduleReader.ReadAsync(fileName, fileStream, null);

            await WorkScheduleValidator.ValidateAsync(workSchedule);

            exception = null;
        }
        catch (Exception ex)
        {
            workSchedule = null;
            exception = ex;
        }
        finally
        {
            await fileStream.DisposeAsync();
        }

        WorkScheduleFile workScheduleFile;

        if (workSchedule == null)
        {
            Debug.Assert(exception != null, "Exception should not be null.");

            workScheduleFile = new WorkScheduleFile(fileName, fileSizeInBytes, exception);
        }
        else
        {
            Debug.Assert(workSchedule != null, "Work schedule should not be null.");

            workScheduleFile = new WorkScheduleFile(fileName, fileSizeInBytes, workSchedule);
        }

        WorkScheduleFiles.Add(workScheduleFile);
    }

    private void OnFileLoadCompleted(IEnumerable<FluentInputFileEventArgs> args)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        _state = State.Idle;

        _loadProgressPercent = 0;

        _loadFileName = string.Empty;
    }

    private async Task OpenSettingsDialogAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        if (_dialog != null || _state != State.Idle)
        {
            return;
        }

        _dialog = await DialogService.ShowPanelAsync<SettingsDialog>(new DialogParameters
        {
            Alignment = HorizontalAlignment.Right,
            PreventDismissOnOverlayClick = true,
            PreventScroll = true,
            OnDialogOpened = EventCallback.Factory.Create<DialogInstance>(this, StateHasChanged)
        });

        await _dialog.Result;

        _dialog = null;
    }

    private void RemoveSelectedFiles()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        if (_dialog != null || _state != State.Idle)
        {
            return;
        }

        if (!_selectedWorkScheduleFiles.Any())
        {
            ToastService.ShowInfo(Localizer[nameof(ResourceKey.NoFilesSelected)]);
            return;
        }

        foreach (var selectedFile in _selectedWorkScheduleFiles.AsEnumerable())
        {
            WorkScheduleFiles.Remove(selectedFile);
        }

        _selectedWorkScheduleFiles = Array.Empty<WorkScheduleFile>();
    }

    private async Task OpenFileDetailsDialogAsync(WorkScheduleFile context)
    {
        if (_dialog != null || _state != State.Idle)
        {
            return;
        }

        _dialog = await DialogService.ShowDialogAsync<FileDetailsDialog>(context, new DialogParameters
        {
            Height = "400px",
            PreventDismissOnOverlayClick = true,
            PreventScroll = true,
            Alignment = HorizontalAlignment.Start,
            OnDialogOpened = EventCallback.Factory.Create<DialogInstance>(this, StateHasChanged)
        });

        await _dialog.Result;

        _dialog = null;
    }

    private async Task ProcessSelectedFilesAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        if (_dialog != null || _state != State.Idle)
        {
            return;
        }

        _state = State.Processing;

        string? zipFilePath = null;

        try
        {
            if (!_selectedWorkScheduleFiles.Any())
            {
                ToastService.ShowInfo(Localizer[nameof(ResourceKey.NoFilesSelected)]);
                return;
            }

            if (_selectedWorkScheduleFiles.Any(file => file.Status != WorkScheduleFile.State.Ok))
            {
                ToastService.ShowWarning(Localizer[nameof(ResourceKey.InvalidFilesSelected)]);
                return;
            }

            zipFilePath = Path.GetTempFileName();

            await using var zipFileStream = File.Open(zipFilePath, FileMode.Open, FileAccess.ReadWrite);

            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create, true))
            {
                var fileNameCounter = new Dictionary<string, int>(32);

                foreach (var file in _selectedWorkScheduleFiles)
                {
                    Debug.Assert(file.WorkSchedule != null, "Work schedule should not be null.");
                    Debug.Assert(file.Status == WorkScheduleFile.State.Ok, "Work schedule file should be valid.");

                    foreach (var timeSheet in file.WorkSchedule.ToTimeSheets())
                    {
                        var timeSheetFileName = await TimeSheetWriter.CreateFileNameAsync(timeSheet);

                        ref var counter = ref CollectionsMarshal
                            .GetValueRefOrAddDefault(fileNameCounter, timeSheetFileName, out var keyExists);

                        if (keyExists)
                        {
                            counter += 1;

                            var timeSheetFileNameSpan = timeSheetFileName.AsSpan();

                            var timeSheetFileNameWithoutExtension =
                                Path.GetFileNameWithoutExtension(timeSheetFileNameSpan);

                            var timeSheetFileNameExtension = Path.GetExtension(timeSheetFileNameSpan);

                            timeSheetFileName =
                                $"{timeSheetFileNameWithoutExtension} ({counter}){timeSheetFileNameExtension}";
                        }

                        var zipArchiveEntry = zipArchive.CreateEntry(timeSheetFileName, CompressionLevel.Optimal);

                        await using var entryStream = zipArchiveEntry.Open();

                        await TimeSheetWriter.WriteAsync(entryStream, timeSheet, SettingsManager.Holidays);

                        await entryStream.FlushAsync();
                    }
                }
            }

            await zipFileStream.FlushAsync();
            zipFileStream.Seek(0, SeekOrigin.Begin);

            using var streamRef = new DotNetStreamReference(stream: zipFileStream, true);

            await Js.InvokeVoidAsync("downloadFileFromStream",
                $"papyrus_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.zip", streamRef);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(Localizer[nameof(ResourceKey.FilesProcessingError)]);
            Logger.LogError(ex, "An error occured during processing.");
        }
        finally
        {
            if (zipFilePath != null && File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            _state = State.Idle;
        }
    }
}