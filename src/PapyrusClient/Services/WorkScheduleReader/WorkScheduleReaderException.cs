namespace PapyrusClient.Services.WorkScheduleReader;

public class WorkScheduleReaderException : LocalizableException
{
    public WorkScheduleReaderException(IStringLocalizer localizer, string resourceKey) : base(localizer, resourceKey)
    {
    }

    public WorkScheduleReaderException(IStringLocalizer localizer, string resourceKey, params string[] resourceArgs) :
        base(localizer, resourceKey, resourceArgs)
    {
    }

    public WorkScheduleReaderException(IStringLocalizer localizer, string resourceKey, Exception inner,
        params string[] resourceArgs) : base(localizer, resourceKey, inner, resourceArgs)
    {
    }
}