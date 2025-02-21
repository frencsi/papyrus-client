using System.Collections.Frozen;
using Microsoft.Extensions.Localization;
using PapyrusClient.Models;
using Sylvan.Data.Excel;
using ResourceKey = PapyrusClient.Resources.Services.WorkScheduleReader.WorkScheduleExcelReader;

namespace PapyrusClient.Services.WorkScheduleReader;

public class WorkScheduleExcelReader(IStringLocalizer<WorkScheduleExcelReader> localizer) : IWorkScheduleReader
{
    private static readonly TimeSpan
        StartContinuationMarkerValue = TimeSpan.Zero,
        EndContinuationMarkerValue = TimeSpan.FromHours(24);

    private static readonly FrozenDictionary<string, ExcelWorkbookType> WorkbookTypes =
        new Dictionary<string, ExcelWorkbookType>(StringComparer.OrdinalIgnoreCase)
        {
            { ".xlsx", ExcelWorkbookType.ExcelXml },
            { ".xlsb", ExcelWorkbookType.ExcelBinary },
            { ".xls", ExcelWorkbookType.Excel }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> FileExtensionsSet = WorkbookTypes
        .Select(pair => pair.Key)
        .ToFrozenSet();

    private static readonly FrozenDictionary<string, WorkType> WorkTypes =
        new Dictionary<string, WorkType>(StringComparer.OrdinalIgnoreCase)
            {
                { "Operator", WorkType.Operator },
                { "Operátor", WorkType.Operator }
            }
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> SupportedFileExtensions => FileExtensionsSet;

    public long MaxFileSizeInBytes => 10 * 1024 * 1024; // 10 MB

    public async Task<WorkSchedule> ReadAsync(string fileName, Stream fileSource, WorkOptions? options,
        CancellationToken cancellationToken = default)
    {
        var workbookType = GetWorkbookType(fileName);

        var excelReaderOptions = new ExcelDataReaderOptions
        {
            OwnsStream = false
        };

        CheckFileSize(fileSource);

        await using var reader = await ExcelDataReader
            .CreateAsync(fileSource, workbookType, excelReaderOptions, cancellationToken);

        // The library skips the first row when reading data.
        // See: https://github.com/MarkPflug/Sylvan.Data.Excel/issues/188
        var company = ParseCompany(reader, 0);

        await reader.ReadAsync(cancellationToken);
        var address = ParseAddress(reader, 0);

        await reader.ReadAsync(cancellationToken);
        var period = ParseYearMonth(reader, 0);

        await reader.ReadAsync(cancellationToken);
        var type = ParseType(reader, 0);

        // Skip a row because it is the column indicators
        await reader.ReadAsync(cancellationToken);

        var shifts = new List<WorkShift>(512);

        // Shifts data start
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = ParseDate(reader, 0);
            var employee = ParseEmployee(reader, 1);
            var startTime = ParseTime(reader, StartContinuationMarkerValue, 2);
            var endTime = ParseTime(reader, EndContinuationMarkerValue, 3);

            var shift = new WorkShift(date, employee, startTime, endTime);

            shifts.Add(shift);
        }

        options ??= type switch
        {
            WorkType.Operator => WorkOptions.DefaultOperator,
            WorkType.Unknown => null,
            _ => null
        };

        return new WorkSchedule(company, address, period, type, shifts)
        {
            Options = options
        };
    }

    private ExcelWorkbookType GetWorkbookType(string fileName)
    {
        var fileExtension = Path.GetExtension(fileName.Trim());

        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MissingFileExtension),
                resourceArgs: [string.Join(", ", SupportedFileExtensions)]);
        }

        foreach (var (extension, type) in WorkbookTypes)
        {
            if (fileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        throw new WorkScheduleExcelReaderException(
            localizer: localizer,
            resourceKey: nameof(ResourceKey.InvalidFileExtension),
            resourceArgs: [fileExtension, string.Join(", ", SupportedFileExtensions)]);
    }

    private void CheckFileSize(Stream fileSource)
    {
        if (fileSource.Length > MaxFileSizeInBytes)
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.FileIsTooLarge),
                resourceArgs: [fileSource.Length.ToString(), MaxFileSizeInBytes.ToString()]);
        }
    }

    private string ParseCompany(ExcelDataReader reader, int column)
    {
        var value = reader.GetName(0).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MissingCompanyName),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString()]);
        }

        return string.Intern(value);
    }

    private string ParseAddress(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MissingAddress),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString()]);
        }

        return string.Intern(value);
    }

    private (int Year, int Month) ParseYearMonth(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MissingYearAndMonth),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString()]);
        }

        var span = value.AsSpan();

        int? year = null;
        int? month = null;

        foreach (var range in span.SplitAny('.', '-', ',', ':', '_'))
        {
            var part = span[range].Trim();

            // Check if the part is a 4-digit year.
            if (year is null && int.TryParse(part, out var yearPart) && yearPart is > 1000 and < 9999)
            {
                year = yearPart;
                continue;
            }

            switch (month)
            {
                // Check if the part is a numeric month (1-12).
                case null when int.TryParse(part, out var numericMonth) && numericMonth is > 0 and <= 12:
                    month = numericMonth;
                    continue;
                // Check if the part is a textual month (e.g., January).
                case null when DateTime.TryParseExact(part, "MMMM", null,
                    System.Globalization.DateTimeStyles.None, out var monthDate):
                    month = monthDate.Month;
                    continue;
                // Check if the part is a textual short month (e.g., Jan).
                case null when DateTime.TryParseExact(part, "MMM", null,
                    System.Globalization.DateTimeStyles.None, out var shortMonthDate):
                    month = shortMonthDate.Month;
                    continue;
            }
        }

        if (year == null)
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.InvalidYearInYearAndMonth),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString(), value]);
        }

        if (month == null)
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.InvalidMonthInYearAndMonth),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString(), value]);
        }

        return (year.Value, month.Value);
    }

    private WorkType ParseType(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MissingWorkType),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString()]);
        }

        if (!WorkTypes.TryGetValue(value, out var type))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.InvalidWorkType),
                resourceArgs:
                [(column + 1).ToString(), reader.RowNumber.ToString(), value, string.Join(", ", WorkTypes.Keys)]);
        }

        return type;
    }

    private DateOnly ParseDate(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MissingDate),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString()]);
        }

        if (!DateOnly.TryParse(value, out var dateOnly))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.InvalidDate),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString(), value]);
        }

        return dateOnly;
    }

    private string ParseEmployee(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WorkScheduleExcelReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MissingEmployeeName),
                resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString()]);
        }

        return string.Intern(value);
    }

    private WorkShift.Time ParseTime(ExcelDataReader reader,
        TimeSpan continuationValue, int column)
    {
        var value = reader.GetString(column).Trim();

        if (value.Equals(WorkOptions.ContinuationMarker, StringComparison.Ordinal))
        {
            return new WorkShift.Time(Value: continuationValue, HasContinuationMarker: true);
        }

        if (DateTime.TryParse(value, out var dateTime))
        {
            return new WorkShift.Time(Value: dateTime.TimeOfDay, HasContinuationMarker: false);
        }

        throw new WorkScheduleExcelReaderException(
            localizer: localizer,
            resourceKey: nameof(ResourceKey.InvalidTime),
            resourceArgs: [(column + 1).ToString(), reader.RowNumber.ToString(), value]);
    }
}