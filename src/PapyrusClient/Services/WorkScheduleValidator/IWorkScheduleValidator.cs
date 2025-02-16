using PapyrusClient.Models;

namespace PapyrusClient.Services.WorkScheduleValidator;

public interface IWorkScheduleValidator
{
    Task ValidateAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default);
}