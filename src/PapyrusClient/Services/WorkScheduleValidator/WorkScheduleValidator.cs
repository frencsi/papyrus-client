using System.Diagnostics;
using System.Runtime.InteropServices;
using ResourceKey = PapyrusClient.Resources.Services.WorkScheduleValidator.WorkScheduleValidator;

namespace PapyrusClient.Services.WorkScheduleValidator;

public class WorkScheduleValidator(IStringLocalizer<WorkScheduleValidator> localizer) : IWorkScheduleValidator
{
    public Task ValidateAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default)
    {
        if (workSchedule.Rule == null)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.RULE_NOT_SET));
        }

        if (workSchedule.State == WorkScheduleState.ReadError)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.CANT_VALIDATE_BECAUSE_READ_ERROR));
        }

        var yearMonthDates = new SortedSet<DateOnly>(collection: workSchedule.YearMonth.GetDates());

        var employeeShiftTimes = new Dictionary<Employee, TimeSpan>(
            capacity: 32,
            comparer: EmployeeComparer.NameOrdinalIgnoreCase);

        IGrouping<DateOnly, WorkShift>? shiftsInPreviousDate = null;

        foreach (var shiftsInDate in workSchedule.Shifts
                     .GroupBy(shift => shift.Date)
                     .OrderBy(grouping => grouping.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureShiftDateMatchesWorkSchedule(workSchedule, shiftsInDate.Key);

            yearMonthDates.Remove(shiftsInDate.Key);

            WorkShift? shiftInPreviousDay = null;

            foreach (var shiftInDate in shiftsInDate
                         .OrderBy(shift => shift.Start.Value)
                         .ThenBy(shift => shift.End.Value))
            {
                cancellationToken.ThrowIfCancellationRequested();

                EnsureShiftHasValidDuration(shiftInDate);

                if (shiftsInPreviousDate != null)
                {
                    ValidateShiftContinuity(workSchedule, shiftsInPreviousDate, shiftInDate);
                }

                ValidateShiftGap(workSchedule, shiftInPreviousDay, shiftInDate);

                ValidateShiftOverlap(workSchedule, shiftInPreviousDay, shiftInDate);

                AddEmployeeShiftTimes(employeeShiftTimes, shiftInDate);

                shiftInPreviousDay = shiftInDate;
            }

            ValidateEmployeeShiftTimes(workSchedule, shiftsInDate.Key, employeeShiftTimes);

            ResetEmployeeShiftTimes(employeeShiftTimes);

            shiftsInPreviousDate = shiftsInDate;
        }

        ValidateRequireShiftsEveryDay(workSchedule, yearMonthDates);

        return Task.CompletedTask;
    }

    private void EnsureShiftDateMatchesWorkSchedule(WorkSchedule workSchedule, DateOnly date)
    {
        if (workSchedule.YearMonth.Year != date.Year)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.SHIFTS_DATE_YEAR_NOT_MATCH_SCHEDULE_YEAR),
                resourceArgs:
                [
                    workSchedule.YearMonth.Year.ToString(),
                    date.Year.ToString(),
                    date.ToString("O")
                ]);
        }

        if (workSchedule.YearMonth.Month != date.Month)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.SHIFTS_DATE_MONTH_NOT_MATCH_SCHEDULE_MONTH),
                resourceArgs:
                [
                    workSchedule.YearMonth.Year.ToString(),
                    date.Year.ToString(),
                    date.ToString("O")
                ]);
        }
    }

    private void EnsureShiftHasValidDuration(WorkShift shift)
    {
        if (shift.ExactDuration() <= TimeSpan.Zero)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.SHIFT_DURATION_IS_NEGATIVE_OR_ZERO),
                resourceArgs:
                [
                    shift.ToString()
                ]);
        }
    }

    private void ValidateShiftContinuity(WorkSchedule workSchedule, IGrouping<DateOnly, WorkShift> shiftsInPreviousDate,
        WorkShift shift)
    {
        Debug.Assert(workSchedule.Rule != null, "Work schedule rule should not be null");

        if (!workSchedule.Rule.ValidateShiftContinuity)
        {
            return;
        }

        if (workSchedule.Type != WorkScheduleType.Operator)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.SHIFT_CONTINUITY_VALIDATION_NOT_SUPPORTED),
                resourceArgs:
                [
                    shift.ToString()
                ]);
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
                EmployeeComparer.NameOrdinalIgnoreCase.Equals(x.Employee, shift.Employee));

        switch (count)
        {
            case 0:
                throw new WorkScheduleValidatorException(
                    localizer: localizer,
                    resourceKey: nameof(ResourceKey.PREVIOUS_SHIFT_MISSING_END_CONTINUATION_MARKER),
                    resourceArgs:
                    [
                        shift.Employee.Name,
                        previousDate.ToString("O"),
                        shift.Date.ToString("O")
                    ]);

            case > 1:
                throw new WorkScheduleValidatorException(
                    localizer: localizer,
                    resourceKey: nameof(ResourceKey.PREVIOUS_SHIFT_MULTIPLE_END_CONTINUATION_MARKER_FOUND),
                    resourceArgs:
                    [
                        shift.Employee.Name,
                        previousDate.ToString("O"),
                        shift.Date.ToString("O")
                    ]);
        }
    }

    private void ValidateShiftGap(WorkSchedule workSchedule, WorkShift? previousShift, WorkShift shift)
    {
        Debug.Assert(workSchedule.Rule != null, "Work schedule rule should not be null");

        if (workSchedule.Rule.AllowGapBetweenShifts)
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
                    resourceKey: nameof(ResourceKey.GAP_DETECTED_BETWEEN_SHIFTS),
                    resourceArgs:
                    [
                        shift.Date.ToString("O"),
                        previousShift.Employee.Name,
                        previousShift.Start.Value.ToString("g"),
                        previousShift.End.Value.ToString("g"),
                        shift.Employee.Name,
                        shift.Start.Value.ToString("g"),
                        shift.End.Value.ToString("g"),
                        gap.ToString("g")
                    ]);
            }
        }
    }

    private void ValidateShiftOverlap(WorkSchedule workSchedule, WorkShift? previousShift, WorkShift shift)
    {
        Debug.Assert(workSchedule.Rule != null, "Work schedule rule should not be null");

        if (workSchedule.Rule.AllowOverlapBetweenShifts)
        {
            return;
        }

        if (previousShift != null &&
            previousShift.End.Value > shift.Start.Value)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.OVERLAP_DETECTED_BETWEEN_SHIFTS),
                resourceArgs:
                [
                    shift.Date.ToString("O"),
                    previousShift.Employee.Name,
                    previousShift.Start.Value.ToString("g"),
                    previousShift.End.Value.ToString("g"),
                    shift.Employee.Name,
                    shift.Start.Value.ToString("g"),
                    shift.End.Value.ToString("g")
                ]);
        }
    }

    private void ValidateEmployeeShiftTimes(WorkSchedule workSchedule, DateOnly date,
        Dictionary<Employee, TimeSpan> employeeShiftTimes)
    {
        Debug.Assert(workSchedule.Rule != null, "Work schedule rule should not be null");

        var combined = TimeSpan.Zero;

        foreach (var employeeShiftTime in employeeShiftTimes)
        {
            if (employeeShiftTime.Value > workSchedule.Rule.MaxShiftDurationEmployeePerDay)
            {
                throw new WorkScheduleValidatorException(
                    localizer: localizer,
                    resourceKey: nameof(ResourceKey.EMPLOYEE_SHIFT_DURATION_IS_GREATER_THAN_RULE),
                    resourceArgs:
                    [
                        employeeShiftTime.Key.Name,
                        employeeShiftTime.Value.ToString("g"),
                        workSchedule.Rule.MaxShiftDurationEmployeePerDay.ToString("g"),
                        date.ToString("O")
                    ]);
            }

            combined += employeeShiftTime.Value;
        }

        if (combined > workSchedule.Rule.MaxShiftDurationCombinedPerDay)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.COMBINED_SHIFTS_DURATION_IS_GREATER_THAN_RULE),
                resourceArgs:
                [
                    combined.ToString("g"),
                    workSchedule.Rule.MaxShiftDurationCombinedPerDay.Value.ToString("g"),
                    date.ToString("O")
                ]);
        }
    }

    private void ValidateRequireShiftsEveryDay(WorkSchedule workSchedule, SortedSet<DateOnly> days)
    {
        Debug.Assert(workSchedule.Rule != null, "Work schedule rule should not be null");

        if (!workSchedule.Rule.RequireShiftsEveryDay)
        {
            return;
        }

        if (days.Count != 0)
        {
            throw new WorkScheduleValidatorException(
                localizer: localizer,
                resourceKey: nameof(ResourceKey.MISSING_REQUIRED_DATES),
                resourceArgs: [string.Join(", ", days.Select(date => date.ToString("O")))]);
        }
    }

    private static void AddEmployeeShiftTimes(Dictionary<Employee, TimeSpan> employeeShiftTimes,
        WorkShift shift)
    {
        ref var timeSpan = ref CollectionsMarshal.GetValueRefOrAddDefault(employeeShiftTimes, shift.Employee, out _);

        timeSpan += shift.ExactDuration();
    }

    private static void ResetEmployeeShiftTimes(Dictionary<Employee, TimeSpan> employeeShiftTimes)
    {
        employeeShiftTimes.Clear();
    }
}