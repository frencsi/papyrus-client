namespace PapyrusClient.Services.WorkScheduleReader;

public class ExcelWorkScheduleReaderException : WorkScheduleReaderException
{
    public ExcelWorkScheduleReaderException(IStringLocalizer<ExcelWorkScheduleReader> localizer, string resourceKey) :
        base(localizer, resourceKey)
    {
    }

    public ExcelWorkScheduleReaderException(IStringLocalizer<ExcelWorkScheduleReader> localizer, string resourceKey,
        params string[] resourceArgs) : base(localizer, resourceKey, resourceArgs)
    {
    }

    public ExcelWorkScheduleReaderException(IStringLocalizer<ExcelWorkScheduleReader> localizer, string resourceKey,
        Exception inner, params string[] resourceArgs) : base(localizer, resourceKey, inner, resourceArgs)
    {
    }
}