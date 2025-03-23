using System.Collections.Frozen;
using Sylvan.Data.Excel;
using ResourceKey = PapyrusClient.Resources.Services.WorkScheduleReader.ExcelWorkScheduleReader;

namespace PapyrusClient.Services.WorkScheduleReader;

public class ExcelWorkScheduleReader(
    TimeProvider timeProvider,
    IStringLocalizer<ExcelWorkScheduleReader> localizer)
    : IWorkScheduleReader
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

    public IReadOnlySet<string> SupportedFileExtensions => FileExtensionsSet;

    public long MaxFileSizeInBytes => 10 * 1024 * 1024; // 10 MB

    public async Task<WorkSchedule> ReadAsync(string fileName, Stream fileSource, WorkScheduleOptions options,
        CancellationToken cancellationToken = default)
    {
        CheckFileSize(fileSource);

        var excelType = GetExcelType(fileName);

        var culture = CultureInfo.CurrentUICulture;

        var excelReaderOptions = new ExcelDataReaderOptions
        {
            OwnsStream = false,
            Culture = culture
        };

        await using var reader = await ExcelDataReader
            .CreateAsync(fileSource, excelType, excelReaderOptions, cancellationToken);

        // The library skips the first row when reading data.
        // See: https://github.com/MarkPflug/Sylvan.Data.Excel/issues/188
        var company = ParseCompany(reader, 0);

        await reader.ReadAsync(cancellationToken);
        var location = ParseLocation(reader, 0);

        await reader.ReadAsync(cancellationToken);
        var yearMonth = ParseYearMonth(reader, 0);

        await reader.ReadAsync(cancellationToken);
        var type = ParseType(reader, 0);

        // Skip a row because it is the column indicators
        await reader.ReadAsync(cancellationToken);

        var shifts = new List<WorkShift>(512);

        // Shifts data start
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = ParseShiftDate(reader, 0);
            var employee = ParseShiftEmployee(reader, 1);
            var startTime = ParseShiftTime(reader, StartContinuationMarkerValue, 2);
            var endTime = ParseShiftTime(reader, EndContinuationMarkerValue, 3);

            var shift = new WorkShift(date, employee, startTime, endTime);

            shifts.Add(shift);
        }

        return new WorkSchedule(
            Company: company,
            Location: location,
            YearMonth: yearMonth,
            Type: type,
            Shifts: shifts,
            Metadata: new WorkScheduleMetadata(fileName, WorkScheduleSource.File, timeProvider.GetLocalNow()),
            Rule: options.GetValidationRuleOrDefault(type),
            Exception: null);
    }

    private ExcelWorkbookType GetExcelType(ReadOnlySpan<char> fileName)
    {
        var fileExtension = Path.GetExtension(fileName.Trim()).Trim();

        if (fileExtension.IsEmpty || fileExtension.IsWhiteSpace())
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_FILE_EXTENSION));
        }

        foreach (var type in WorkbookTypes)
        {
            if (fileExtension.Equals(type.Key, StringComparison.OrdinalIgnoreCase))
            {
                return type.Value;
            }
        }

        throw new ExcelWorkScheduleReaderException(
            localizer: localizer,
            resourceKey: nameof(ResourceKey.UNSUPPORTED_FILE_EXTENSION),
            resourceArgs: new string(fileExtension));
    }

    private void CheckFileSize(Stream fileSource)
    {
        if (fileSource.Length > MaxFileSizeInBytes)
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.FILE_TOO_LARGE),
                resourceArgs:
                [
                    fileSource.Length.ToString(),
                    MaxFileSizeInBytes.ToString()
                ]);
        }
    }

    private Company ParseCompany(ExcelDataReader reader, int column)
    {
        var value = reader.GetName(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_COMPANY_NAME),
                resourceArgs:
                [
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        return new Company(string.Intern(value));
    }

    private Location ParseLocation(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_LOCATION_ADDRESS),
                resourceArgs:
                [
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        return new Location(string.Intern(value));
    }

    private YearMonth ParseYearMonth(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).AsMemory().Trim();

        if (value.IsEmpty)
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_YEARMONTH),
                resourceArgs:
                [
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        var span = value.Span;

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
                    DateTimeStyles.None, out var monthDate):
                    month = monthDate.Month;
                    continue;
                // Check if the part is a textual short month (e.g., Jan).
                case null when DateTime.TryParseExact(part, "MMM", null,
                    DateTimeStyles.None, out var shortMonthDate):
                    month = shortMonthDate.Month;
                    continue;
            }
        }

        if (year == null)
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.INVALID_YEAR_IN_YEARMONTH),
                resourceArgs:
                [
                    value.ToString(),
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        if (month == null)
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.INVALID_MONTH_IN_YEARMONTH),
                resourceArgs:
                [
                    value.ToString(),
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        return new YearMonth(year.Value, month.Value);
    }

    private WorkScheduleType ParseType(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).AsMemory().Trim();

        if (value.IsEmpty)
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_SCHEDULE_TYPE),
                resourceArgs:
                [
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        var valueSpan = value.Span;

        const char separator = ',';

        var localizeSpan = localizer[nameof(ResourceKey.SCHEDULE_TYPE_OPERATOR_ACCEPTABLE_VALUES)].Value
            .AsSpan()
            .Trim();

        foreach (var range in localizeSpan.Split(separator))
        {
            if (valueSpan.Equals(localizeSpan[range].Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return WorkScheduleType.Operator;
            }
        }

        throw new ExcelWorkScheduleReaderException(
            localizer: localizer,
            resourceKey: nameof(ResourceKey.INVALID_SCHEDULE_TYPE),
            resourceArgs:
            [
                value.ToString(),
                (column + 1).ToString(),
                reader.RowNumber.ToString()
            ]);
    }

    private DateOnly ParseShiftDate(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).AsMemory().Trim();

        if (value.IsEmpty)
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_SHIFT_DATE),
                resourceArgs:
                [
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        if (!DateOnly.TryParse(value.Span, out var dateOnly))
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.INVALID_SHIFT_DATE),
                resourceArgs:
                [
                    value.ToString(),
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        return dateOnly;
    }

    private Employee ParseShiftEmployee(ExcelDataReader reader, int column)
    {
        var value = reader.GetString(column).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_SHIFT_EMPLOYEE),
                resourceArgs:
                [
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        return new Employee(string.Intern(value));
    }

    private WorkShiftTime ParseShiftTime(ExcelDataReader reader, TimeSpan continuationValue, int column)
    {
        var value = reader.GetString(column).AsMemory().Trim();

        if (value.IsEmpty)
        {
            throw new ExcelWorkScheduleReaderException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_SHIFT_TIME),
                resourceArgs:
                [
                    (column + 1).ToString(),
                    reader.RowNumber.ToString()
                ]);
        }

        var span = value.Span;

        if (WorkScheduleOptions.ContinuationMarker.AsSpan().Equals(span, StringComparison.Ordinal))
        {
            return new WorkShiftTime(Value: continuationValue, HasContinuationMarker: true);
        }

        if (DateTime.TryParse(span, out var dateTime))
        {
            return new WorkShiftTime(Value: dateTime.TimeOfDay, HasContinuationMarker: false);
        }

        throw new ExcelWorkScheduleReaderException(
            localizer: localizer,
            resourceKey: nameof(ResourceKey.INVALID_SHIFT_TIME),
            resourceArgs:
            [
                value.ToString(),
                (column + 1).ToString(),
                reader.RowNumber.ToString()
            ]);
    }
}