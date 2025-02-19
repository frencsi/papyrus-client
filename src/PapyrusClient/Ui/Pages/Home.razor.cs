using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using PapyrusClient.Models;
using PapyrusClient.Services.SettingsManager;
using PapyrusClient.Ui.Dialogs;

namespace PapyrusClient.Ui.Pages;

public partial class Home : ComponentBase, IAsyncDisposable
{
    private enum Status : byte
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

    private Status _state = Status.Idle;

    private int _loadProgressPercent = 0;

    private string _loadFileName = string.Empty;

    private IDialogReference? _dialog;

    #endregion

    protected override void OnInitialized()
    {
        base.OnInitialized();

        SettingsManager.CultureChanged += OnCultureChanged;

        SettingsManager.ThemeChanged += OnThemeChanged;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        SettingsManager.CultureChanged -= OnCultureChanged;

        SettingsManager.ThemeChanged -= OnThemeChanged;

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

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnFileCountExceeded(int count)
    {
        ToastService.ShowWarning($"You can only load {MaximumFileCountPerLoad} files at once.");
    }

    private async Task OnFileLoadedAsync(FluentInputFileEventArgs args)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        Debug.Assert(args.Stream != null, "Stream should not be null");

        _state = Status.Loading;

        _loadProgressPercent = args.ProgressPercent;

        _loadFileName = args.Name;

        var fileStream = args.Stream;

        var fileName = args.Name;
        var fileSizeInBytes = args.Size;
        WorkSchedule? workSchedule;
        WorkScheduleFileStatus fileStatus;

        try
        {
            workSchedule = await WorkScheduleReader.ReadAsync(fileName, fileStream, null);

            await WorkScheduleValidator.ValidateAsync(workSchedule);

            fileStatus = WorkScheduleFileStatus.Ok();
        }
        catch (Exception ex)
        {
            workSchedule = null;
            fileStatus = WorkScheduleFileStatus.Error(ex);
        }
        finally
        {
            await fileStream.DisposeAsync();
        }

        var workScheduleFile = new WorkScheduleFile
        {
            Name = fileName,
            SizeInyBytes = fileSizeInBytes,
            WorkSchedule = workSchedule,
            Status = fileStatus
        };

        WorkScheduleFiles.Add(workScheduleFile);
    }

    private void OnFileLoadCompleted(IEnumerable<FluentInputFileEventArgs> args)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        _state = Status.Idle;

        _loadProgressPercent = 0;

        _loadFileName = string.Empty;
    }

    private async Task OpenSettingsDialogAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        if (_dialog != null || _state != Status.Idle)
        {
            return;
        }

        _dialog = await DialogService.ShowPanelAsync<SettingsDialog>(new DialogParameters
        {
            Alignment = HorizontalAlignment.Right,
            Title = "Settings",
            PrimaryAction = "Save",
            SecondaryAction = "Discard",
            PreventDismissOnOverlayClick = true,
            OnDialogOpened = EventCallback.Factory.Create<DialogInstance>(this, StateHasChanged)
        });

        await _dialog.Result;

        _dialog = null;
    }

    private void RemoveSelectedFiles()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(Home));

        if (_dialog != null || _state != Status.Idle)
        {
            return;
        }

        if (!_selectedWorkScheduleFiles.Any())
        {
            ToastService.ShowInfo("Please select at least one file.");
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
        if (_dialog != null || _state != Status.Idle)
        {
            return;
        }

        _dialog = await DialogService.ShowDialogAsync<FileDetailsDialog>(context, new DialogParameters
        {
            Height = "400px",
            Title = "File details",
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

        if (_dialog != null || _state != Status.Idle)
        {
            return;
        }

        _state = Status.Processing;

        string? zipFilePath = null;

        try
        {
            if (!_selectedWorkScheduleFiles.Any())
            {
                ToastService.ShowInfo("Please select at least one file.");
                return;
            }

            if (_selectedWorkScheduleFiles.Any(file => file.Status.State != WorkScheduleFileState.Ok))
            {
                ToastService.ShowWarning("Only files with valid data can be processed.");
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
                    Debug.Assert(file.Status.State == WorkScheduleFileState.Ok, "Work schedule file should be valid.");

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
                $"timeSheets_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.zip", streamRef);
        }
        catch (Exception ex)
        {
            ToastService.ShowError("An error occured during processing. Please see the console for more details.");
            Logger.LogError(ex, "An error occured during processing.");
        }
        finally
        {
            if (zipFilePath != null && File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            _state = Status.Idle;
        }
    }
}