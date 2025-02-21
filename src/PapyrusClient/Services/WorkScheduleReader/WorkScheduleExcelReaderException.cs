using Microsoft.Extensions.Localization;
using PapyrusClient.Utilities;

namespace PapyrusClient.Services.WorkScheduleReader;

public class WorkScheduleExcelReaderException : LocalizableException
{
    public WorkScheduleExcelReaderException(IStringLocalizer<WorkScheduleExcelReader> localizer, string resourceKey)
        : base(localizer, resourceKey)
    {
    }

    public WorkScheduleExcelReaderException(IStringLocalizer<WorkScheduleExcelReader> localizer, string resourceKey,
        params string[] resourceArgs)
        : base(localizer, resourceKey, resourceArgs)
    {
    }

    public WorkScheduleExcelReaderException(IStringLocalizer<WorkScheduleExcelReader> localizer, string resourceKey,
        Exception inner, params string[] resourceArgs)
        : base(localizer, resourceKey, inner, resourceArgs)
    {
    }
}