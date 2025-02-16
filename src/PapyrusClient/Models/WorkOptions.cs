namespace PapyrusClient.Models;

public record WorkOptions
{
    public const string ContinuationMarker = "\u2192";

    public static readonly WorkOptions DefaultOperator = new()
    {
        RequireShiftsEveryDay = true,
        ValidateShiftContinuity = true,
        AllowGapBetweenShifts = false,
        AllowOverlapBetweenShifts = false,
        MaxShiftDurationEmployeePerDay = TimeSpan.FromHours(24),
        MaxShiftDurationCombinedPerDay = TimeSpan.FromHours(24)
    };

    public required bool RequireShiftsEveryDay { get; init; }

    public required bool ValidateShiftContinuity { get; init; }

    public required bool AllowGapBetweenShifts { get; init; }

    public required bool AllowOverlapBetweenShifts { get; init; }

    public required TimeSpan? MaxShiftDurationEmployeePerDay { get; init; }

    public required TimeSpan? MaxShiftDurationCombinedPerDay { get; init; }
}