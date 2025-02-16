using System.Runtime.InteropServices;
using PapyrusClient.Models;

namespace PapyrusClient.Services.WorkScheduleValidator;

public class WorkScheduleValidator : IWorkScheduleValidator
{
    private const string DateFormat = "O";

    public Task ValidateAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default)
    {
        if (workSchedule.Options == null)
        {
            throw new WorkScheduleValidatorException
            {
                Details = """
                          There is no options set for this work schedule.

                          Please provide the options.
                          """
            };
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

    private static void ValidateShiftsDateIsInMonth(DateOnly shiftsDate, WorkSchedule workSchedule)
    {
        if (workSchedule.YearMonth.Year != shiftsDate.Year)
        {
            throw new WorkScheduleValidatorException
            {
                Details = $"""
                           One or more shift dates are not in the header specified year '{workSchedule.YearMonth.Year}'.

                           Please review the following date: '{shiftsDate.ToString(DateFormat)}'.
                           """
            };
        }

        if (workSchedule.YearMonth.Month != shiftsDate.Month)
        {
            throw new WorkScheduleValidatorException
            {
                Details = $"""
                           One or more shift dates are not in the header specified month '{workSchedule.YearMonth.Month}'.

                           Please review the following date: '{shiftsDate.ToString(DateFormat)}'.
                           """
            };
        }
    }

    private static void ValidatePositiveShiftDuration(WorkShift shift)
    {
        if (shift.Duration <= TimeSpan.Zero)
        {
            throw new WorkScheduleValidatorException
            {
                Details = $"""
                           Shift duration can't be negative or zero. 

                           Details:
                           - Shift date: '{shift.Date.ToString(DateFormat)}'
                           - Employee: '{shift.Employee}'
                           - Start: '{shift.Start.Value}'
                           - End: '{shift.End.Value}'
                           - Duration: '{shift.Duration}'
                           """
            };
        }
    }

    private static void ValidateShiftContinuity(WorkShift shift, IGrouping<DateOnly, WorkShift> shiftsInPreviousDate,
        WorkSchedule workSchedule, WorkOptions options)
    {
        if (!options.ValidateShiftContinuity)
        {
            return;
        }

        if (workSchedule.Type != WorkType.Operator)
        {
            throw new WorkScheduleValidatorException
            {
                Details = """
                          Only the operator work type currently supports gap validation.
                          """
            };
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
                throw new WorkScheduleValidatorException
                {
                    Details = $"""
                               A required 'end' continuation marker is missing for the employee '{shift.Employee}' on '{previousDate.ToString(DateFormat)}'.

                               This marker is needed because the shift on '{shift.Date.ToString(DateFormat)}' for the same employee starts with an 'start' continuation marker.

                               Please review the following dates: '{shift.Date.ToString(DateFormat)}' and '{previousDate.ToString(DateFormat)}'.
                               """
                };
            case > 1:
                throw new WorkScheduleValidatorException
                {
                    Details = $"""
                               Duplicated 'end' continuation markers were found for employee '{shift.Employee}' on '{previousDate.ToString(DateFormat)}'.

                               Please review the following dates: '{shift.Date.ToString(DateFormat)}' and '{previousDate.ToString(DateFormat)}'.
                               """
                };
        }
    }

    private static void ValidateShiftGap(WorkShift shift, WorkShift? previousShift, WorkOptions options)
    {
        if (options.AllowGapBetweenShifts)
        {
            return;
        }

        if (previousShift != null &&
            previousShift.End.Value > shift.Start.Value)
        {
            throw new WorkScheduleValidatorException
            {
                Details = $"""
                           There is an overlap in shifts on '{shift.Date.ToString(DateFormat)}'.

                           Details:
                           - Previous Shift: Employee '{previousShift.Employee}' from '{previousShift.Start.Value}' to '{previousShift.End.Value}'
                           - Next Shift: Employee '{shift.Employee}' from '{shift.Start.Value}' to '{shift.End.Value}'

                           Please review the timings for overlapping shifts.
                           """
            };
        }
    }

    private static void ValidateShiftOverlap(WorkShift shift, WorkShift? previousShift, WorkOptions options)
    {
        if (options.AllowOverlapBetweenShifts)
        {
            return;
        }

        if (previousShift != null)
        {
            var gap = shift.Start.Value - previousShift.End.Value;

            if (gap != TimeSpan.Zero)
            {
                throw new WorkScheduleValidatorException
                {
                    Details = $"""
                               A gap was detected between shifts on '{shift.Date.ToString(DateFormat)}'.

                               Details:
                               - Previous Shift: Employee '{previousShift.Employee}' from '{previousShift.Start.Value}' to '{previousShift.End.Value}'
                               - Next Shift: Employee '{shift.Employee}' from '{shift.Start.Value}' to '{shift.End.Value}'
                               - Gap: '{gap}'

                               Please ensure that no gaps exist between shifts.
                               """
                };
            }
        }
    }

    private static void ValidateRequireShiftsEveryDay(SortedSet<DateOnly> periodDates, WorkOptions options)
    {
        if (!options.RequireShiftsEveryDay)
        {
            return;
        }

        if (periodDates.Count != 0)
        {
            throw new WorkScheduleValidatorException
            {
                Details = $"""
                           Shift data is missing for the following days: [{string.Join(", ", periodDates.Select(date => date.ToString(DateFormat)))}].
                           """
            };
        }
    }

    private static void ValidateShiftDurationsInDate(DateOnly date,
        Dictionary<string, TimeSpan> employeesShiftDuration, WorkOptions options)
    {
        var combinedTotalShiftDuration = TimeSpan.Zero;

        foreach (var employeeShiftDuration in employeesShiftDuration)
        {
            if (options.MaxShiftDurationEmployeePerDay != null &&
                employeeShiftDuration.Value > options.MaxShiftDurationEmployeePerDay)
            {
                throw new WorkScheduleValidatorException
                {
                    Details = $"""
                               Employee '{employeeShiftDuration.Key}' logged '{employeeShiftDuration.Value}' time, which exceeds the maximum allowed limit of '{options.MaxShiftDurationEmployeePerDay}'.

                               Please review the following date: '{date.ToString(DateFormat)}'.
                               """
                };
            }

            combinedTotalShiftDuration += employeeShiftDuration.Value;
        }

        if (options.MaxShiftDurationCombinedPerDay != null &&
            combinedTotalShiftDuration > options.MaxShiftDurationCombinedPerDay)
        {
            throw new WorkScheduleValidatorException
            {
                Details = $"""
                           The combined total shift time '{combinedTotalShiftDuration}' exceeded the allowed maximum of '{options.MaxShiftDurationCombinedPerDay}'.

                           Please review the following date: '{date.ToString(DateFormat)}'.
                           """
            };
        }
    }
}