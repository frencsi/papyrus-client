﻿using System.Buffers;
using System.Collections.Frozen;
using System.Drawing;
using Microsoft.Extensions.Localization;
using PapyrusClient.Models;
using SpreadCheetah;
using SpreadCheetah.Styling;
using SpreadCheetah.Worksheets;
using ResourceKey = PapyrusClient.Resources.Services.TimeSheetWriter.TimeSheetExcelWriter;

namespace PapyrusClient.Services.TimeSheetWriter;

public class TimeSheetExcelWriter(IStringLocalizer<TimeSheetExcelWriter> localizer) : ITimeSheetWriter
{
    private readonly record struct BodyData(
        TimeSpan MonthlyWorkingDuration,
        TimeSpan TotalWorkedDuration,
        TimeSpan WorkedInHolidayDuration,
        TimeSpan OvertimeDuration);

    private const string
        FileExtension = ".xlsx",
        DayOfWeekFormat = "dddd",
        MonthFormat = "MMMM",
        TimeSpanFormat = @"hh\:mm";

    private static readonly FrozenDictionary<WorkType, string> WorkTypeResourceKeyMap =
        new Dictionary<WorkType, string>
        {
            { WorkType.Unknown, nameof(ResourceKey.WorkTypeUnknown) },
            { WorkType.Operator, nameof(ResourceKey.WorkTypeOperator) }
        }.ToFrozenDictionary();

    private static readonly Color BorderColor = Color.Black;

    #region Header cell styles

    private static readonly Style TopCellHeaderStyle = new()
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
            },
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style MiddleCellHeaderStyle = new()
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

    private static readonly Style BottomCellHeaderStyle = new()
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
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style HeaderColumnStyle = new()
    {
        Font = new Font
        {
            Bold = true,
        },
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
            },
            Bottom = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
            Top = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            },
        }
    };

    #endregion

    #region Body cell styles

    private static readonly Style StartCellBodyStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Left,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
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

    private static readonly Style DayCellBodyStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Center,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
        },
        Border = new Border
        {
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Right = new EdgeBorder
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

    private static readonly Style MiddleCellBodyStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Center,
            Vertical = VerticalAlignment.Center
        },
        Border = new Border
        {
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Right = new EdgeBorder
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

    private static readonly Style EndCellBodyStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Center,
            Vertical = VerticalAlignment.Center
        },
        Border = new Border
        {
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Thin,
            },
            Right = new EdgeBorder
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

    #endregion

    #region Footer cell styles

    private static readonly Style TopCellFooterStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Center,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
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
            },
            Top = new EdgeBorder
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

    private static readonly Style MiddleLeftCellFooterStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Left,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
        },
        Border = new Border
        {
            Left = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style MiddleRightCellFooterStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Left,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
        },
        Border = new Border
        {
            Right = new EdgeBorder
            {
                Color = BorderColor,
                BorderStyle = BorderStyle.Medium,
            }
        }
    };

    private static readonly Style MiddleLeftSeparatorCellFooterStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Left,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
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
                BorderStyle = BorderStyle.Thin,
            },
        }
    };

    private static readonly Style MiddleRightSeparatorCellFooterStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Left,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
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
                BorderStyle = BorderStyle.Thin,
            }
        }
    };


    private static readonly Style BottomLeftCellFooterStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Left,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
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

    private static readonly Style BottomRightCellFooterStyle = new()
    {
        Alignment = new Alignment
        {
            Horizontal = HorizontalAlignment.Left,
            Vertical = VerticalAlignment.Center
        },
        Font = new Font
        {
            Bold = true
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
            }
        }
    };

    #endregion

    private static readonly IReadOnlyList<(string ResourceKey, double Width)> Columns =
    [
        (nameof(ResourceKey.DayColumn), 12.00),
        (nameof(ResourceKey.DateColumn), 9.00),
        (nameof(ResourceKey.ArriveColumn), 9.00),
        (nameof(ResourceKey.LeaveColumn), 9.00),
        (nameof(ResourceKey.DayPeriodColumn), 9.00),
        (nameof(ResourceKey.AfternoonPeriodColumn), 9.00),
        (nameof(ResourceKey.NightPeriodColumn), 9.00),
        (nameof(ResourceKey.SummaryColumn), 11.00),
        (nameof(ResourceKey.NoteColumn), 10.00)
    ];

    public Task<string> CreateFileNameAsync(TimeSheet timeSheet,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileName =
            $"{localizer[nameof(ResourceKey.FileNamePart)]} {localizer[WorkTypeResourceKeyMap[timeSheet.Type]]} {timeSheet.YearMonth.Year}{timeSheet.YearMonth.Month} {timeSheet.Employee}{FileExtension}";

        return Task.FromResult(fileName);
    }

    public async Task WriteAsync(Stream destination, TimeSheet timeSheet, IReadOnlySet<DateOnly> holidays,
        CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<Cell>.Shared.Rent(9);

        var memory = buffer.AsMemory()[..9];
        memory.Span.Clear();

        try
        {
            await using var spreadsheet = await Spreadsheet
                .CreateNewAsync(destination, cancellationToken: cancellationToken);

            await CreateHeaderAsync(spreadsheet, timeSheet, memory, cancellationToken);

            var bodyData = await CreateBodyAsync(spreadsheet, timeSheet, holidays, memory, cancellationToken);

            await CreateFooterAsync(spreadsheet, timeSheet, memory, bodyData, cancellationToken);

            await spreadsheet.FinishAsync(cancellationToken);
        }
        finally
        {
            ArrayPool<Cell>.Shared.Return(buffer);
        }
    }

    private async Task CreateHeaderAsync(Spreadsheet spreadsheet, TimeSheet timeSheet, Memory<Cell> buffer,
        CancellationToken cancellationToken = default)
    {
        var topCellHeaderStyleId = spreadsheet.AddStyle(TopCellHeaderStyle);
        var middleCellHeaderStyleId = spreadsheet.AddStyle(MiddleCellHeaderStyle);
        var bottomCellHeaderStyleId = spreadsheet.AddStyle(BottomCellHeaderStyle);
        var headerColumnStyleId = spreadsheet.AddStyle(HeaderColumnStyle);

        var worksheetOptions = new WorksheetOptions();

        for (var i = 0; i < Columns.Count; i++)
        {
            worksheetOptions.Column(i + 1).Width = Columns[i].Width;
        }

        await spreadsheet.StartWorksheetAsync($"{timeSheet.YearMonth.Year}-{timeSheet.YearMonth.Month}",
            options: worksheetOptions,
            token: cancellationToken);

        for (var i = 1; i < buffer.Span.Length; i++)
        {
            buffer.Span[i] = new Cell(new DataCell(), topCellHeaderStyleId);
        }

        buffer.Span[0] = new Cell(timeSheet.Company, topCellHeaderStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        for (var i = 1; i < buffer.Length; i++)
        {
            buffer.Span[i] = new Cell(new DataCell(), middleCellHeaderStyleId);
        }

        buffer.Span[0] = new Cell(timeSheet.Location, middleCellHeaderStyleId);
        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] =
            new Cell(
                $"{localizer[nameof(ResourceKey.FileNamePart)]} - {localizer[WorkTypeResourceKeyMap[timeSheet.Type]]}",
                middleCellHeaderStyleId);
        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] = new Cell(timeSheet.Employee, middleCellHeaderStyleId);
        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        for (var i = 1; i < buffer.Length; i++)
        {
            buffer.Span[i] = new Cell(new DataCell(), bottomCellHeaderStyleId);
        }

        buffer.Span[0] = new Cell($"{timeSheet.YearMonth.Year}. {timeSheet.FirstDateOfYearMonth.ToString(MonthFormat)}",
            bottomCellHeaderStyleId);
        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer.Span[i] = new Cell(localizer[Columns[i].ResourceKey], headerColumnStyleId);
        }

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);
    }

    private async Task<BodyData> CreateBodyAsync(Spreadsheet spreadsheet, TimeSheet timeSheet,
        IReadOnlySet<DateOnly> holidays, Memory<Cell> buffer, CancellationToken cancellationToken = default)
    {
        var monthlyWorkingDuration = TimeSpan.Zero;
        var totalWorkedDuration = TimeSpan.Zero;
        var workedInHolidayDuration = TimeSpan.Zero;
        var overTimeDuration = TimeSpan.Zero;

        var startCellBodyStyleId = spreadsheet.AddStyle(StartCellBodyStyle);
        var dayCellBodyStyleId = spreadsheet.AddStyle(DayCellBodyStyle);
        var middleCellBodyStyleId = spreadsheet.AddStyle(MiddleCellBodyStyle);
        var endCellBodyStyleId = spreadsheet.AddStyle(EndCellBodyStyle);

        var tbdMarker = localizer[nameof(ResourceKey.TbdMarker)];
        var holidayMarker = localizer[nameof(ResourceKey.HolidayMarker)];

        foreach (var periodDate in timeSheet.GetYearMonthDates())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isEmpty = true;

            if (!holidays.Contains(periodDate) &&
                periodDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                monthlyWorkingDuration += TimeSpan.FromHours(8);
            }

            foreach (var shiftInPeriodDate in timeSheet.Shifts
                         .Where(shift => shift.Date == periodDate)
                         .OrderBy(shift => shift.Start.Value)
                         .ThenBy(shift => shift.End.Value))
            {
                isEmpty = false;

                totalWorkedDuration += shiftInPeriodDate.Duration;

                buffer.Span[0] = new Cell(periodDate.ToString(DayOfWeekFormat), startCellBodyStyleId);
                buffer.Span[1] = new Cell(periodDate.Day, dayCellBodyStyleId);

                if (shiftInPeriodDate.Start.HasContinuationMarker)
                {
                    buffer.Span[2] = new Cell(WorkOptions.ContinuationMarker, middleCellBodyStyleId);
                }
                else
                {
                    buffer.Span[2] = new Cell(shiftInPeriodDate.Start.Value.ToString(TimeSpanFormat),
                        middleCellBodyStyleId);
                }

                if (shiftInPeriodDate.End.HasContinuationMarker)
                {
                    buffer.Span[3] = new Cell(WorkOptions.ContinuationMarker, middleCellBodyStyleId);
                }
                else
                {
                    buffer.Span[3] = new Cell(shiftInPeriodDate.End.Value.ToString(TimeSpanFormat),
                        middleCellBodyStyleId);
                }

                buffer.Span[4] = new Cell(tbdMarker, middleCellBodyStyleId);
                buffer.Span[5] = new Cell(tbdMarker, middleCellBodyStyleId);
                buffer.Span[6] = new Cell(tbdMarker, middleCellBodyStyleId);

                buffer.Span[7] = new Cell(shiftInPeriodDate.Duration.TotalHours, middleCellBodyStyleId);

                if (holidays.Contains(periodDate))
                {
                    buffer.Span[8] = new Cell(holidayMarker, endCellBodyStyleId);

                    workedInHolidayDuration += shiftInPeriodDate.Duration;
                }
                else
                {
                    buffer.Span[8] = new Cell(new DataCell(), endCellBodyStyleId);
                }


                await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);
            }

            if (!isEmpty)
            {
                continue;
            }

            buffer.Span[0] = new Cell(periodDate.ToString(DayOfWeekFormat), startCellBodyStyleId);
            buffer.Span[1] = new Cell(periodDate.Day, dayCellBodyStyleId);

            buffer.Span[2] = new Cell(new DataCell(), middleCellBodyStyleId);
            buffer.Span[3] = new Cell(new DataCell(), middleCellBodyStyleId);
            buffer.Span[4] = new Cell(new DataCell(), middleCellBodyStyleId);
            buffer.Span[5] = new Cell(new DataCell(), middleCellBodyStyleId);
            buffer.Span[6] = new Cell(new DataCell(), middleCellBodyStyleId);
            buffer.Span[7] = new Cell(new DataCell(), middleCellBodyStyleId);
            buffer.Span[8] = new Cell(new DataCell(), endCellBodyStyleId);

            await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);
        }

        if (totalWorkedDuration > monthlyWorkingDuration)
        {
            overTimeDuration = totalWorkedDuration - monthlyWorkingDuration;
        }

        return new BodyData(
            MonthlyWorkingDuration: monthlyWorkingDuration,
            TotalWorkedDuration: totalWorkedDuration,
            WorkedInHolidayDuration: workedInHolidayDuration,
            OvertimeDuration: overTimeDuration);
    }

    private async Task CreateFooterAsync(Spreadsheet spreadsheet, TimeSheet timeSheet, Memory<Cell> buffer,
        BodyData bodyData, CancellationToken cancellationToken = default)
    {
        var topCellFooterStyleId = spreadsheet.AddStyle(TopCellFooterStyle);

        var middleLeftCellFooterStyleId = spreadsheet.AddStyle(MiddleLeftCellFooterStyle);
        var middleRightCellFooterStyleId = spreadsheet.AddStyle(MiddleRightCellFooterStyle);

        var middleLeftSeparatorCellFooterStyleId = spreadsheet.AddStyle(MiddleLeftSeparatorCellFooterStyle);
        var middleRightSeparatorCellFooterStyleId = spreadsheet.AddStyle(MiddleRightSeparatorCellFooterStyle);

        var bottomLeftCellFooterStyleId = spreadsheet.AddStyle(BottomLeftCellFooterStyle);
        var bottomRightCellFooterStyleId = spreadsheet.AddStyle(BottomRightCellFooterStyle);

        var tbdMarker = localizer[nameof(ResourceKey.TbdMarker)];

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FooterSummary)], topCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), topCellFooterStyleId);

        buffer.Span[4] = new Cell(tbdMarker, topCellFooterStyleId);
        buffer.Span[5] = new Cell(tbdMarker, topCellFooterStyleId);
        buffer.Span[6] = new Cell(tbdMarker, topCellFooterStyleId);
        buffer.Span[7] = new Cell(bodyData.TotalWorkedDuration.TotalHours, topCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), topCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FooterNote)], topCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), topCellFooterStyleId);

        buffer.Span[4] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[5] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[6] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[7] = new Cell(new DataCell(), topCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), topCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FooterMonthlyHours)], middleLeftCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), middleLeftCellFooterStyleId);

        buffer.Span[4] = new Cell(bodyData.MonthlyWorkingDuration.TotalHours, middleRightCellFooterStyleId);
        buffer.Span[5] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[6] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[7] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), middleRightCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");
        spreadsheet.MergeCells($"E{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FooterWorkedHours)],
            middleLeftSeparatorCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), middleLeftSeparatorCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), middleLeftSeparatorCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), middleLeftSeparatorCellFooterStyleId);

        buffer.Span[4] = new Cell(bodyData.TotalWorkedDuration.TotalHours, middleRightSeparatorCellFooterStyleId);
        buffer.Span[5] = new Cell(new DataCell(), middleRightSeparatorCellFooterStyleId);
        buffer.Span[6] = new Cell(new DataCell(), middleRightSeparatorCellFooterStyleId);
        buffer.Span[7] = new Cell(new DataCell(), middleRightSeparatorCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), middleRightSeparatorCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");
        spreadsheet.MergeCells($"E{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FooterWorkedHolidayHours)], middleLeftCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), middleLeftCellFooterStyleId);

        buffer.Span[4] = new Cell(bodyData.WorkedInHolidayDuration.TotalHours, middleRightCellFooterStyleId);
        buffer.Span[5] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[6] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[7] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), middleRightCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");
        spreadsheet.MergeCells($"E{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FooterPaidLeaveHours)], middleLeftCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), middleLeftCellFooterStyleId);

        buffer.Span[4] = new Cell(0D, middleRightCellFooterStyleId);
        buffer.Span[5] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[6] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[7] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), middleRightCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");
        spreadsheet.MergeCells($"E{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        buffer.Span[0] = new Cell(localizer[nameof(ResourceKey.FooterOvertimeHours)], middleLeftCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), middleLeftCellFooterStyleId);

        buffer.Span[4] = new Cell(bodyData.OvertimeDuration.TotalHours, middleRightCellFooterStyleId);
        buffer.Span[5] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[6] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[7] = new Cell(new DataCell(), middleRightCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), middleRightCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");
        spreadsheet.MergeCells($"E{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);

        for (var i = 0; i < 2; i++)
        {
            buffer.Span[0] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
            buffer.Span[1] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
            buffer.Span[2] = new Cell(new DataCell(), middleLeftCellFooterStyleId);
            buffer.Span[3] = new Cell(new DataCell(), middleLeftCellFooterStyleId);

            buffer.Span[4] = new Cell(new DataCell(), middleRightCellFooterStyleId);
            buffer.Span[5] = new Cell(new DataCell(), middleRightCellFooterStyleId);
            buffer.Span[6] = new Cell(new DataCell(), middleRightCellFooterStyleId);
            buffer.Span[7] = new Cell(new DataCell(), middleRightCellFooterStyleId);
            buffer.Span[8] = new Cell(new DataCell(), middleRightCellFooterStyleId);

            spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");
            spreadsheet.MergeCells($"E{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

            await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);
        }

        buffer.Span[0] = new Cell(new DataCell(), bottomLeftCellFooterStyleId);
        buffer.Span[1] = new Cell(new DataCell(), bottomLeftCellFooterStyleId);
        buffer.Span[2] = new Cell(new DataCell(), bottomLeftCellFooterStyleId);
        buffer.Span[3] = new Cell(new DataCell(), bottomLeftCellFooterStyleId);

        buffer.Span[4] = new Cell(new DataCell(), bottomRightCellFooterStyleId);
        buffer.Span[5] = new Cell(new DataCell(), bottomRightCellFooterStyleId);
        buffer.Span[6] = new Cell(new DataCell(), bottomRightCellFooterStyleId);
        buffer.Span[7] = new Cell(new DataCell(), bottomRightCellFooterStyleId);
        buffer.Span[8] = new Cell(new DataCell(), bottomRightCellFooterStyleId);

        spreadsheet.MergeCells($"A{spreadsheet.NextRowNumber}:D{spreadsheet.NextRowNumber}");
        spreadsheet.MergeCells($"E{spreadsheet.NextRowNumber}:I{spreadsheet.NextRowNumber}");

        await spreadsheet.AddRowAsync(buffer[..9], cancellationToken);
    }
}