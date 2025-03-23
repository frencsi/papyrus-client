namespace PapyrusClient.Services.WorkScheduleValidator;

public class WorkScheduleValidatorException : LocalizableException
{
    public WorkScheduleValidatorException(IStringLocalizer<WorkScheduleValidator> localizer, string resourceKey)
        : base(localizer, resourceKey)
    {
    }

    public WorkScheduleValidatorException(IStringLocalizer<WorkScheduleValidator> localizer, string resourceKey,
        params string[] resourceArgs)
        : base(localizer, resourceKey, resourceArgs)
    {
    }

    public WorkScheduleValidatorException(IStringLocalizer<WorkScheduleValidator> localizer, string resourceKey,
        Exception inner, params string[] resourceArgs)
        : base(localizer, resourceKey, inner, resourceArgs)
    {
    }
}