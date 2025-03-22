using System.Drawing;
using System.IO.Compression;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using Humanizer;
using SpreadCheetah;
using SpreadCheetah.Styling;
using SpreadCheetah.Worksheets;
using ResourceKey = PapyrusClient.Resources.Services.TimeSheetWriter.ExcelTimeSheetWriter;

namespace PapyrusClient.Services.TimeSheetWriter;

public class ExcelTimeSheetWriter(
    TimeProvider timeProvider,
    IStringLocalizer<ExcelTimeSheetWriter> localizer)
    : ITimeSheetWriter
{
    private const string TimeSpanFormat = @"hh\:mm";

    private static readonly IReadOnlyList<(string Value, double Width)> Columns =
    [
        (nameof(ResourceKey.COLUMN_DAY), 12.00),
        (nameof(ResourceKey.COLUMN_DATE), 9.00),
        (nameof(ResourceKey.COLUMN_ARRIVE), 9.00),
        (nameof(ResourceKey.COLUMN_LEAVE), 9.00),
        (nameof(ResourceKey.COLUMN_DAY_PERIOD), 9.00),
        (nameof(ResourceKey.COLUMN_AFTERNOON_PERIOD), 9.00),
        (nameof(ResourceKey.COLUMN_NIGHT_PERIOD), 9.00),
        (nameof(ResourceKey.COLUMN_SUMMARY), 11.00),
        (nameof(ResourceKey.COLUMN_NOTE), 10.00)
    ];

    public async Task<string> WriteAsZipAsync(IEnumerable<WorkSchedule> workSchedules, Holidays holidays,
        Stream zipStream, bool leaveOpen, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workSchedules);
        ArgumentNullException.ThrowIfNull(holidays);
        ArgumentNullException.ThrowIfNull(zipStream);

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen);

        var fileNameCounter = new Dictionary<string, int>(32);

        foreach (var workSchedule in workSchedules)
        {
            if (workSchedule.State != WorkScheduleState.Ok)
            {
                throw new ArgumentException(
                    $"A work schedule that is not '{WorkScheduleState.Ok:G}' can't be written.",
                    nameof(workSchedules));
            }

            foreach (var employeeShifts in workSchedule.Shifts.GroupBy(
                         shift => shift.Employee,
                         comparer: EmployeeComparer.NameOrdinalIgnoreCase))
            {
                var excelFileName = CreateExcelFileName(workSchedule, employeeShifts.Key, fileNameCounter);

                var archiveEntry = archive.CreateEntry(excelFileName, CompressionLevel.Optimal);

                await using var archiveEntryStream = archiveEntry.Open();

                await CreateExcelAsync(workSchedule, holidays, employeeShifts, archiveEntryStream,
                    cancellationToken);

                await archiveEntryStream.FlushAsync(cancellationToken);
            }
        }

        return $"papyrus_{timeProvider.GetLocalNow().ToUnixTimeSeconds()}.zip";
    }

    private async Task CreateExcelAsync(WorkSchedule workSchedule, Holidays holidays,
        IGrouping<Employee, WorkShift> employeeShifts, Stream destination,
        CancellationToken cancellationToken = default)
    {
        await using var spreadsheet = await Spreadsheet
            .CreateNewAsync(destination, cancellationToken: cancellationToken);

        await CreateWorksheetAsync(spreadsheet, workSchedule, cancellationToken);

        await CreateStylesAsync(spreadsheet, cancellationToken);

        await CreateHeaderAsync(spreadsheet, workSchedule, employeeShifts.Key, cancellationToken);

        await CreateHeaderColumnsAsync(spreadsheet, cancellationToken);

        var bodyData = await CreateBodyAsync(spreadsheet, workSchedule, holidays, employeeShifts, cancellationToken);

        await CreateFooterColumnsAsync(spreadsheet, bodyData, cancellationToken);

        await CreateFooterAsync(spreadsheet, bodyData, cancellationToken);

        await spreadsheet.FinishAsync(cancellationToken);
    }

    private async Task CreateFooterAsync(Spreadsheet spreadsheet, BodyData bodyData,
        CancellationToken cancellationToken = default)
    {
        var footerTopLeftStyle = spreadsheet.GetStyleId(nameof(FooterTopLeftStyle));
        var footerTopRightStyle = spreadsheet.GetStyleId(nameof(FooterTopRightStyle));
        var footerMiddleLeftStyle = spreadsheet.GetStyleId(nameof(FooterMiddleLeftStyle));
        var footerMiddleRightStyle = spreadsheet.GetStyleId(nameof(FooterMiddleRightStyle));
        var footerBottomLeftStyle = spreadsheet.GetStyleId(nameof(FooterBottomLeftStyle));
        var footerBottomRightStyle = spreadsheet.GetStyleId(nameof(FooterBottomRightStyle));

        await AddRowAsync(spreadsheet, new DataCell(localizer[nameof(ResourceKey.FOOTER_MONTH_WORK_TIME)]),
            footerTopLeftStyle, new DataCell(HumanizeToHour(bodyData.MonthlyWorkTime)), footerTopRightStyle,
            cancellationToken);

        await AddRowAsync(spreadsheet, new DataCell(localizer[nameof(ResourceKey.FOOTER_ACTUAL_WORKED_HOURS)]),
            footerMiddleLeftStyle, new DataCell(HumanizeToHour(bodyData.OverallWorkedTime)), footerMiddleRightStyle,
            cancellationToken);

        await AddRowAsync(spreadsheet, new DataCell(localizer[nameof(ResourceKey.FOOTER_WORKED_HOURS_ON_HOLIDAY)]),
            footerMiddleLeftStyle,
            new DataCell(bodyData.HolidayWorkTime != TimeSpan.Zero
                ? HumanizeToHour(bodyData.HolidayWorkTime)
                : string.Empty), footerMiddleRightStyle, cancellationToken);

        await AddRowAsync(spreadsheet, new DataCell(localizer[nameof(ResourceKey.FOOTER_PAID_LEAVE)]),
            footerMiddleLeftStyle, new DataCell(string.Empty), footerMiddleRightStyle, cancellationToken);

        await AddRowAsync(spreadsheet, new DataCell(localizer[nameof(ResourceKey.FOOTER_OVERTIME)]),
            footerMiddleLeftStyle, new DataCell(HumanizeToHour(bodyData.OvertimeWorkTime)), footerMiddleRightStyle,
            cancellationToken);

        for (var i = 0; i < 2; i++)
        {
            await AddRowAsync(spreadsheet, new DataCell(string.Empty), footerMiddleLeftStyle,
                new DataCell(string.Empty), footerMiddleRightStyle, cancellationToken);
        }

        await AddRowAsync(spreadsheet, new DataCell(string.Empty), footerBottomLeftStyle,
            new DataCell(string.Empty), footerBottomRightStyle, cancellationToken);

        return;

        static async ValueTask AddRowAsync(Spreadsheet spreadsheet, DataCell left, StyleId leftStyleId, DataCell right,
            StyleId rightStyleId, CancellationToken cancellationToken = default)
        {
            const string cellRangeLeft = "A{0}:D{0}";
            const string cellRangeRight = "E{0}:I{0}";

            using var bufferOwner = MemoryOwner<Cell>.Allocate(Columns.Count, AllocationMode.Clear);
            var buffer = bufferOwner.Memory;

            buffer.Span[0] = new Cell(left, leftStyleId);
            buffer.Span[1] = new Cell(string.Empty, leftStyleId);
            buffer.Span[2] = new Cell(string.Empty, leftStyleId);
            buffer.Span[3] = new Cell(string.Empty, leftStyleId);

            buffer.Span[4] = new Cell(right, rightStyleId);
            buffer.Span[5] = new Cell(string.Empty, rightStyleId);
            buffer.Span[6] = new Cell(string.Empty, rightStyleId);
            buffer.Span[7] = new Cell(string.Empty, rightStyleId);
            buffer.Span[8] = new Cell(string.Empty, rightStyleId);

            spreadsheet.MergeCells(string.Format(cellRangeLeft, spreadsheet.NextRowNumber));
            spreadsheet.MergeCells(string.Format(cellRangeRight, spreadsheet.NextRowNumber));

            await spreadsheet.AddRowAsync(buffer, cancellationToken);
        }
    }

    private async Task CreateFooterColumnsAsync(Spreadsheet spreadsheet, BodyData bodyData,
        CancellationToken cancellationToken = default)
    {
        const string cellRange = "A{0}:D{0}";

        var footerColumnStyle = spreadsheet.GetStyleId(nameof(FooterColumnStyle));

        using var bufferOwner = MemoryOwner<Cell>.Allocate(Columns.Count, AllocationMode.Clear);
        var buffer = bufferOwner.Memory;

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FOOTER_COLUMN_SUMMARY)], footerColumnStyle);
        buffer.Span[1] = new Cell(string.Empty, footerColumnStyle);
        buffer.Span[2] = new Cell(string.Empty, footerColumnStyle);
        buffer.Span[3] = new Cell(string.Empty, footerColumnStyle);

        buffer.Span[4] = new Cell(localizer[nameof(ResourceKey.VALUE_TBD)], footerColumnStyle);
        buffer.Span[5] = new Cell(localizer[nameof(ResourceKey.VALUE_TBD)], footerColumnStyle);
        buffer.Span[6] = new Cell(localizer[nameof(ResourceKey.VALUE_TBD)], footerColumnStyle);
        buffer.Span[7] = new Cell(bodyData.MonthlyWorkTime.TotalMinutes, footerColumnStyle);
        buffer.Span[8] = new Cell(string.Empty, footerColumnStyle);

        spreadsheet.MergeCells(string.Format(cellRange, spreadsheet.NextRowNumber));

        await spreadsheet.AddRowAsync(buffer, cancellationToken);
    }

    private async Task<BodyData> CreateBodyAsync(Spreadsheet spreadsheet, WorkSchedule workSchedule,
        Holidays holidays, IGrouping<Employee, WorkShift> employeeShifts, CancellationToken cancellationToken = default)
    {
        var monthlyWorkTime = TimeSpan.Zero;
        var overallWorkedTime = TimeSpan.Zero;
        var holidayWorkTime = TimeSpan.Zero;
        var overtimeWorkTime = TimeSpan.Zero;

        foreach (var date in workSchedule.YearMonth.GetDates())
        {
            var isHoliday = holidays.Dates.Contains(date);
            var hasShift = false;

            if (isHoliday is false && date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                monthlyWorkTime += TimeSpan.FromHours(8);
            }

            foreach (var shift in employeeShifts
                         .Where(shift => shift.Date == date)
                         .OrderBy(shift => shift.Start.Value)
                         .ThenBy(shift => shift.End.Value))
            {
                var shiftRoundedDuration = shift.RoundedDuration();

                hasShift = true;

                overallWorkedTime += shiftRoundedDuration;

                if (isHoliday)
                {
                    holidayWorkTime += shiftRoundedDuration;
                }

                await WriteRowAsync(spreadsheet, date, isHoliday, shift, localizer, cancellationToken);
            }

            if (!hasShift)
            {
                await WriteRowAsync(spreadsheet, date, isHoliday, null, localizer, cancellationToken);
            }
        }

        if (overallWorkedTime > monthlyWorkTime)
        {
            overtimeWorkTime = overallWorkedTime - monthlyWorkTime;
        }

        return new BodyData(
            MonthlyWorkTime: monthlyWorkTime,
            OverallWorkedTime: overallWorkedTime,
            HolidayWorkTime: holidayWorkTime,
            OvertimeWorkTime: overtimeWorkTime);

        static async ValueTask WriteRowAsync(Spreadsheet spreadsheet, DateOnly date, bool isHoliday, WorkShift? shift,
            IStringLocalizer<ExcelTimeSheetWriter> localizer,
            CancellationToken cancellationToken = default)
        {
            var bodyStartStyle = spreadsheet.GetStyleId(nameof(BodyStartStyle));
            var bodyMiddleStyle = spreadsheet.GetStyleId(nameof(BodyMiddleStyle));
            var bodyEndStyle = spreadsheet.GetStyleId(nameof(BodyEndStyle));

            using var bufferOwner = MemoryOwner<Cell>.Allocate(Columns.Count, AllocationMode.Clear);
            var buffer = bufferOwner.Memory;

            buffer.Span[0] = new Cell(date.ToString("dddd"), bodyStartStyle);

            buffer.Span[1] = new Cell(date.Day, bodyMiddleStyle);

            if (shift != null)
            {
                var shiftRoundedDuration = shift.RoundedDuration();

                buffer.Span[2] = new Cell(shift.Start.HasContinuationMarker
                    ? WorkScheduleOptions.ContinuationMarker
                    : shift.Start.Value.ToString(TimeSpanFormat), bodyMiddleStyle);

                buffer.Span[3] = new Cell(shift.End.HasContinuationMarker
                    ? WorkScheduleOptions.ContinuationMarker
                    : shift.End.Value.ToString(TimeSpanFormat), bodyMiddleStyle);

                buffer.Span[4] = new Cell(localizer[nameof(ResourceKey.VALUE_TBD)], bodyMiddleStyle);

                buffer.Span[5] = new Cell(localizer[nameof(ResourceKey.VALUE_TBD)], bodyMiddleStyle);

                buffer.Span[6] = new Cell(localizer[nameof(ResourceKey.VALUE_TBD)], bodyMiddleStyle);

                buffer.Span[7] = new Cell(shiftRoundedDuration.TotalMinutes, bodyMiddleStyle);
            }
            else
            {
                buffer.Span[2] = new Cell(string.Empty, bodyMiddleStyle);
                buffer.Span[3] = new Cell(string.Empty, bodyMiddleStyle);
                buffer.Span[4] = new Cell(string.Empty, bodyMiddleStyle);
                buffer.Span[5] = new Cell(string.Empty, bodyMiddleStyle);
                buffer.Span[6] = new Cell(string.Empty, bodyMiddleStyle);
                buffer.Span[7] = new Cell(string.Empty, bodyMiddleStyle);
            }

            var note = isHoliday ? localizer[nameof(ResourceKey.VALUE_HOLIDAY)] : string.Empty;

            buffer.Span[8] = new Cell(note, bodyEndStyle);

            await spreadsheet.AddRowAsync(buffer, cancellationToken);
        }
    }

    private async Task CreateHeaderColumnsAsync(Spreadsheet spreadsheet,
        CancellationToken cancellationToken = default)
    {
        var headerColumnStyle = spreadsheet.GetStyleId(nameof(HeaderColumnStyle));

        using var bufferOwner = MemoryOwner<Cell>.Allocate(Columns.Count, AllocationMode.Clear);
        var buffer = bufferOwner.Memory;

        for (var i = 0; i < Columns.Count; i++)
        {
            buffer.Span[i] = new Cell(localizer[Columns[i].Value], headerColumnStyle);
        }

        await spreadsheet.AddRowAsync(buffer, cancellationToken);
    }

    private async Task CreateHeaderAsync(Spreadsheet spreadsheet, WorkSchedule workSchedule, Employee employee,
        CancellationToken cancellationToken = default)
    {
        var headerTopStyle = spreadsheet.GetStyleId(nameof(HeaderTopStyle));
        var headerMiddleStyle = spreadsheet.GetStyleId(nameof(HeaderMiddleStyle));
        var headerBottomStyle = spreadsheet.GetStyleId(nameof(HeaderBottomStyle));

        await WriteRowAsync(spreadsheet, new Cell(workSchedule.Company.Name, headerTopStyle), cancellationToken);

        await WriteRowAsync(spreadsheet, new Cell(workSchedule.Location.Address, headerMiddleStyle), cancellationToken);

        await WriteRowAsync(spreadsheet, new Cell(employee.Name, headerMiddleStyle), cancellationToken);

        await WriteRowAsync(spreadsheet,
            new Cell($"{localizer[nameof(ResourceKey.TIMESHEET)]} - {GetLocalizedWorksheetType(workSchedule.Type)}",
                headerMiddleStyle), cancellationToken);

        var yearMonthString =
            $"{workSchedule.YearMonth.Year}. {CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(workSchedule.YearMonth.Month)}";

        await WriteRowAsync(spreadsheet, new Cell(yearMonthString, headerBottomStyle),
            cancellationToken);

        return;

        static async ValueTask WriteRowAsync(Spreadsheet spreadsheet, Cell value,
            CancellationToken cancellationToken = default)
        {
            const string cellRange = "A{0}:I{0}";

            using var bufferOwner = MemoryOwner<Cell>.Allocate(Columns.Count, AllocationMode.Clear);
            var buffer = bufferOwner.Memory;

            buffer.Span[0] = value;

            for (var i = 1; i < buffer.Length; i++)
            {
                buffer.Span[i] = value;
            }

            spreadsheet.MergeCells(string.Format(cellRange, spreadsheet.NextRowNumber));

            await spreadsheet.AddRowAsync(buffer, cancellationToken);
        }
    }

    private static Task CreateStylesAsync(Spreadsheet spreadsheet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = spreadsheet.AddStyle(HeaderTopStyle, nameof(HeaderTopStyle));
        _ = spreadsheet.AddStyle(HeaderMiddleStyle, nameof(HeaderMiddleStyle));
        _ = spreadsheet.AddStyle(HeaderBottomStyle, nameof(HeaderBottomStyle));
        _ = spreadsheet.AddStyle(HeaderColumnStyle, nameof(HeaderColumnStyle));
        _ = spreadsheet.AddStyle(BodyStartStyle, nameof(BodyStartStyle));
        _ = spreadsheet.AddStyle(BodyMiddleStyle, nameof(BodyMiddleStyle));
        _ = spreadsheet.AddStyle(BodyEndStyle, nameof(BodyEndStyle));
        _ = spreadsheet.AddStyle(FooterColumnStyle, nameof(FooterColumnStyle));
        _ = spreadsheet.AddStyle(FooterTopLeftStyle, nameof(FooterTopLeftStyle));
        _ = spreadsheet.AddStyle(FooterTopRightStyle, nameof(FooterTopRightStyle));
        _ = spreadsheet.AddStyle(FooterMiddleLeftStyle, nameof(FooterMiddleLeftStyle));
        _ = spreadsheet.AddStyle(FooterMiddleRightStyle, nameof(FooterMiddleRightStyle));
        _ = spreadsheet.AddStyle(FooterBottomLeftStyle, nameof(FooterBottomLeftStyle));
        _ = spreadsheet.AddStyle(FooterBottomRightStyle, nameof(FooterBottomRightStyle));

        return Task.CompletedTask;
    }

    private static async Task CreateWorksheetAsync(Spreadsheet spreadsheet, WorkSchedule workSchedule,
        CancellationToken cancellationToken = default)
    {
        var worksheetOptions = new WorksheetOptions();

        for (var i = 0; i < Columns.Count; i++)
        {
            worksheetOptions.Column(i + 1).Width = Columns[i].Width;
        }

        var worksheetName = $"{workSchedule.YearMonth.Year}-{workSchedule.YearMonth.Month}";

        await spreadsheet.StartWorksheetAsync(worksheetName, worksheetOptions, cancellationToken);
    }

    #region Styles

    private static readonly Color BorderColor = Color.Black;

    private static readonly Style FooterBottomRightStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Left
        },
        Border = new Border
        {
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            }
        }
    };

    private static readonly Style FooterBottomLeftStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Left
        },
        Border = new Border
        {
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style FooterMiddleRightStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Left
        },
        Border = new Border
        {
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            }
        }
    };

    private static readonly Style FooterMiddleLeftStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Left
        },
        Border = new Border
        {
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            }
        }
    };

    private static readonly Style FooterTopLeftStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Left
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
        }
    };

    private static readonly Style FooterTopRightStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Left
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style FooterColumnStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Center
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style BodyEndStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Center
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style BodyMiddleStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Center
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            }
        }
    };

    private static readonly Style BodyStartStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Left
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            }
        }
    };

    private static readonly Style HeaderColumnStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Center
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style HeaderTopStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Center
        },
        Border = new Border
        {
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style HeaderMiddleStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Center
        },
        Border = new Border
        {
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style HeaderBottomStyle = new()
    {
        Alignment = new Alignment
        {
            Vertical = VerticalAlignment.Center,
            Horizontal = HorizontalAlignment.Center
        },
        Border = new Border
        {
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    #endregion

    #region Helpers

    private readonly record struct BodyData(
        TimeSpan MonthlyWorkTime,
        TimeSpan OverallWorkedTime,
        TimeSpan HolidayWorkTime,
        TimeSpan OvertimeWorkTime
    );

    private static string HumanizeToHour(TimeSpan timeSpan)
    {
        return timeSpan.Humanize(minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Hour);
    }

    private string CreateExcelFileName(WorkSchedule workSchedule, Employee employee,
        Dictionary<string, int> fileNameCounter)
    {
        const string fileExtension = ".xlsx";

        var fileName =
            $"{localizer[ResourceKey.TIMESHEET]} {GetLocalizedWorksheetType(workSchedule.Type)} {workSchedule.YearMonth.Year}{workSchedule.YearMonth.Month} {employee.Name}{fileExtension}";

        ref var counter = ref CollectionsMarshal
            .GetValueRefOrAddDefault(fileNameCounter, fileName, out var keyExists);

        if (!keyExists)
        {
            return fileName;
        }

        counter += 1;

        var span = fileName.AsSpan();

        var withoutExtension =
            Path.GetFileNameWithoutExtension(span);

        var extension = Path.GetExtension(span);

        fileName = $"{withoutExtension} ({counter}){extension}";

        return fileName;
    }

    private string GetLocalizedWorksheetType(WorkScheduleType workScheduleType)
    {
        return workScheduleType switch
        {
            WorkScheduleType.Operator => localizer[ResourceKey.TYPE_OPERATOR],
            WorkScheduleType.Unknown => localizer[ResourceKey.TYPE_UNKNOWN],
            _ => localizer[ResourceKey.TYPE_UNKNOWN],
        };
    }

    #endregion
}