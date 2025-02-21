using System.Runtime.InteropServices;
using Microsoft.Extensions.Localization;
using PapyrusClient.Models;
using ResourceKey = PapyrusClient.Resources.Services.WorkScheduleValidator.WorkScheduleValidator;

namespace PapyrusClient.Services.WorkScheduleValidator;

public class WorkScheduleValidator(IStringLocalizer<WorkScheduleValidator> localizer) : IWorkScheduleValidator
{
    private const string
        DateFormat = "O",
        TimeSpanFormat = "g";

    public Task ValidateAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default)
    {
        if (workSchedule.Options == null)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.NoOptionsSet));
        }

        var yearMonthDates = new SortedSet<DateOnly>(workSchedule.GetYearMonthDates());

        var employeesShiftDurationInDate = new Dictionary<string, TimeSpan>(32, StringComparer.OrdinalIgnoreCase);

        IGrouping<DateOnly, WorkShift>? shiftsInPreviousDate = null;

        foreach (var shiftsInDate in workSchedule.Shifts
                     .GroupBy(shift => shift.Date)
                     .OrderBy(group => group.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateShiftsDateIsInMonth(shiftsInDate.Key, workSchedule);

            yearMonthDates.Remove(shiftsInDate.Key);

            employeesShiftDurationInDate.Clear();

            WorkShift? previousShift = null;

            foreach (var shiftInDate in shiftsInDate
                         .OrderBy(shift => shift.Start.Value)
                         .ThenBy(shift => shift.End.Value))
            {
                cancellationToken.ThrowIfCancellationRequested();

                ValidatePositiveShiftDuration(shiftInDate);

                if (shiftsInPreviousDate != null)
                {
                    ValidateShiftContinuity(shiftInDate, shiftsInPreviousDate, workSchedule, workSchedule.Options);
                }

                ValidateShiftGap(shiftInDate, previousShift, workSchedule.Options);

                ValidateShiftOverlap(shiftInDate, previousShift, workSchedule.Options);

                ref var timeSpan = ref CollectionsMarshal
                    .GetValueRefOrAddDefault(employeesShiftDurationInDate, shiftInDate.Employee, out _);

                timeSpan += shiftInDate.Duration;

                previousShift = shiftInDate;
            }

            ValidateShiftDurationsInDate(shiftsInDate.Key, employeesShiftDurationInDate, workSchedule.Options);

            shiftsInPreviousDate = shiftsInDate;
        }

        ValidateRequireShiftsEveryDay(yearMonthDates, workSchedule.Options);

        return Task.CompletedTask;
    }

    private void ValidateShiftsDateIsInMonth(DateOnly shiftsDate, WorkSchedule workSchedule)
    {
        if (workSchedule.YearMonth.Year != shiftsDate.Year)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.ShiftsDateNotInSpecifiedYear),
                resourceArgs: [workSchedule.YearMonth.Year.ToString(), shiftsDate.ToString(DateFormat)]);
        }

        if (workSchedule.YearMonth.Month != shiftsDate.Month)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.ShiftsDateNotInSpecifiedMonth),
                resourceArgs: [workSchedule.YearMonth.Year.ToString(), shiftsDate.ToString(DateFormat)]);
        }
    }

    private void ValidatePositiveShiftDuration(WorkShift shift)
    {
        if (shift.Duration <= TimeSpan.Zero)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.ShiftDurationNegativeOrZero),
                resourceArgs: [shift.ToString()]);
        }
    }

    private void ValidateShiftContinuity(WorkShift shift, IGrouping<DateOnly, WorkShift> shiftsInPreviousDate,
        WorkSchedule workSchedule, WorkOptions options)
    {
        if (!options.ValidateShiftContinuity)
        {
            return;
        }

        if (workSchedule.Type != WorkType.Operator)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.WorTypeGapValidationNotSupported));
        }

        if (!shift.Start.HasContinuationMarker)
        {
            return;
        }

        var previousDate = shiftsInPreviousDate.Key;

        var count = shiftsInPreviousDate
            .Count(x =>
                x.Date == previousDate &&
                x.End.HasContinuationMarker &&
                x.Employee.Equals(shift.Employee, StringComparison.OrdinalIgnoreCase));

        switch (count)
        {
            case 0:
                throw new WorkScheduleValidatorException(
                    localizer: localizer,
                    resourceKey: nameof(ResourceKey.PreviousShiftMissingEndContinuationMarker),
                    resourceArgs: [shift.Employee, previousDate.ToString(DateFormat), shift.Date.ToString(DateFormat)]);
            case > 1:
                throw new WorkScheduleValidatorException(
                    localizer: localizer,
                    resourceKey: nameof(ResourceKey.PreviousShiftMultipleEndContinuationMarkerFound),
                    resourceArgs: [shift.Employee, previousDate.ToString(DateFormat), shift.Date.ToString(DateFormat)]);
        }
    }

    private void ValidateShiftGap(WorkShift shift, WorkShift? previousShift, WorkOptions options)
    {
        if (options.AllowGapBetweenShifts)
        {
            return;
        }

        if (previousShift != null)
        {
            var gap = shift.Start.Value - previousShift.End.Value;

            if (gap != TimeSpan.Zero)
            {
                throw new WorkScheduleValidatorException(
                    localizer: localizer,
                    resourceKey: nameof(ResourceKey.ShiftGapDetected),
                    resourceArgs:
                    [
                        shift.Date.ToString(DateFormat),
                        previousShift.Employee,
                        previousShift.Start.Value.ToString(TimeSpanFormat),
                        previousShift.End.Value.ToString(TimeSpanFormat),
                        shift.Employee,
                        shift.Start.Value.ToString(TimeSpanFormat),
                        shift.End.Value.ToString(TimeSpanFormat),
                        gap.ToString(TimeSpanFormat)
                    ]);
            }
        }
    }

    private void ValidateShiftOverlap(WorkShift shift, WorkShift? previousShift, WorkOptions options)
    {
        if (options.AllowOverlapBetweenShifts)
        {
            return;
        }

        if (previousShift != null &&
            previousShift.End.Value > shift.Start.Value)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.ShiftOverlapDetected),
                resourceArgs:
                [
                    shift.Date.ToString(DateFormat),
                    previousShift.Employee,
                    previousShift.Start.Value.ToString(TimeSpanFormat),
                    previousShift.End.Value.ToString(TimeSpanFormat),
                    shift.Employee,
                    shift.Start.Value.ToString(TimeSpanFormat),
                    shift.End.Value.ToString(TimeSpanFormat)
                ]);
        }
    }

    private void ValidateRequireShiftsEveryDay(SortedSet<DateOnly> periodDates, WorkOptions options)
    {
        if (!options.RequireShiftsEveryDay)
        {
            return;
        }

        if (periodDates.Count != 0)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.ShiftMissingRequiredDays),
                resourceArgs: [string.Join(", ", periodDates.Select(date => date.ToString(DateFormat)))]);
        }
    }

    private void ValidateShiftDurationsInDate(DateOnly date,
        Dictionary<string, TimeSpan> employeesShiftDuration, WorkOptions options)
    {
        var combinedTotalShiftDuration = TimeSpan.Zero;

        foreach (var employeeShiftDuration in employeesShiftDuration)
        {
            if (options.MaxShiftDurationEmployeePerDay != null &&
                employeeShiftDuration.Value > options.MaxShiftDurationEmployeePerDay)
            {
                throw new WorkScheduleValidatorException(
                    localizer: localizer,
                    resourceKey: nameof(ResourceKey.ShiftExceedsMaxDurationPerEmployeePerDay),
                    resourceArgs:
                    [
                        employeeShiftDuration.Key, employeeShiftDuration.Value.ToString(TimeSpanFormat),
                        options.MaxShiftDurationEmployeePerDay.Value.ToString(TimeSpanFormat), date.ToString(DateFormat)
                    ]);
            }

            combinedTotalShiftDuration += employeeShiftDuration.Value;
        }

        if (options.MaxShiftDurationCombinedPerDay != null &&
            combinedTotalShiftDuration > options.MaxShiftDurationCombinedPerDay)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.ShiftExceedsMaxDurationCombinedPerDay),
                resourceArgs:
                [
                    combinedTotalShiftDuration.ToString(TimeSpanFormat),
                    options.MaxShiftDurationCombinedPerDay.Value.ToString(TimeSpanFormat), date.ToString(DateFormat)
                ]);
        }
    }
}