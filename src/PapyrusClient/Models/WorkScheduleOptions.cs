using System.Collections.Frozen;

namespace PapyrusClient.Models;

public record WorkScheduleValidationRule(
    bool RequireShiftsEveryDay,
    bool ValidateShiftContinuity,
    bool AllowGapBetweenShifts,
    bool AllowOverlapBetweenShifts,
    TimeSpan MaxShiftDurationEmployeePerDay,
    TimeSpan? MaxShiftDurationCombinedPerDay);

public record WorkScheduleOptions
{
    public const string ContinuationMarker = "\u2192";

    public static readonly WorkScheduleOptions Default =
        new(new Dictionary<WorkScheduleType, WorkScheduleValidationRule>
        {
            {
                WorkScheduleType.Operator, new WorkScheduleValidationRule(
                    RequireShiftsEveryDay: true,
                    ValidateShiftContinuity: true,
                    AllowGapBetweenShifts: false,
                    AllowOverlapBetweenShifts: false,
                    MaxShiftDurationEmployeePerDay: TimeSpan.FromHours(24),
                    MaxShiftDurationCombinedPerDay: TimeSpan.FromHours(24))
            }
        }.ToFrozenDictionary());

    private readonly IReadOnlyDictionary<WorkScheduleType, WorkScheduleValidationRule> _validationRules;

    public WorkScheduleOptions(IEnumerable<(WorkScheduleType Type, WorkScheduleValidationRule Rule)> validationRules)
    {
        _validationRules = validationRules.ToFrozenDictionary(pair => pair.Type, pair => pair.Rule);
    }

    public WorkScheduleOptions(IReadOnlyDictionary<WorkScheduleType, WorkScheduleValidationRule> validationRules)
    {
        _validationRules = validationRules.ToFrozenDictionary();
    }

    public WorkScheduleValidationRule GetValidationRuleOrDefault(WorkScheduleType type)
    {
        return _validationRules.TryGetValue(type, out var rule)
            ? rule
            : Default._validationRules[type];
    }

    public IEnumerable<(WorkScheduleType Type, WorkScheduleValidationRule Rule)> GetValidationRules()
    {
        return _validationRules
            .Select(pair => (pair.Key, pair.Value));
    }
}