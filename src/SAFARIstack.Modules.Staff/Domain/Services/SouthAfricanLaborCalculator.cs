namespace SAFARIstack.Modules.Staff.Domain.Services;

/// <summary>
/// South African BCEA (Basic Conditions of Employment Act) compliance calculator
/// </summary>
public class SouthAfricanLaborCalculator
{
    // SA Public Holidays (2026)
    private static readonly List<DateTime> PublicHolidays2026 = new()
    {
        new DateTime(2026, 1, 1),   // New Year's Day
        new DateTime(2026, 3, 21),  // Human Rights Day
        new DateTime(2026, 4, 3),   // Good Friday
        new DateTime(2026, 4, 6),   // Family Day
        new DateTime(2026, 4, 27),  // Freedom Day
        new DateTime(2026, 5, 1),   // Workers' Day
        new DateTime(2026, 6, 16),  // Youth Day
        new DateTime(2026, 8, 9),   // National Women's Day (Sunday, observed Monday 10th)
        new DateTime(2026, 8, 10),  // Women's Day Observed
        new DateTime(2026, 9, 24),  // Heritage Day
        new DateTime(2026, 12, 16), // Day of Reconciliation
        new DateTime(2026, 12, 25), // Christmas Day
        new DateTime(2026, 12, 26)  // Day of Goodwill
    };

    /// <summary>
    /// Calculate overtime according to BCEA regulations
    /// </summary>
    public OvertimeCalculation CalculateOvertime(
        DateTime checkIn,
        DateTime checkOut,
        decimal scheduledHours,
        decimal hourlyRate)
    {
        var totalHours = (decimal)(checkOut - checkIn).TotalHours;
        var breakHours = CalculateRequiredBreakHours(totalHours);
        var actualWorkHours = totalHours - breakHours;

        var result = new OvertimeCalculation
        {
            TotalHours = totalHours,
            BreakHours = breakHours,
            ActualWorkHours = actualWorkHours,
            ScheduledHours = scheduledHours,
            HourlyRate = hourlyRate
        };

        // Daily overtime: Over 9 hours per day (BCEA Section 9)
        if (actualWorkHours > 9)
        {
            result.DailyOvertimeHours = actualWorkHours - 9;
            result.DailyOvertimeRate = 1.5m; // Time and a half
        }

        // Sunday work: Double time (BCEA Section 11)
        if (checkIn.DayOfWeek == DayOfWeek.Sunday)
        {
            result.SundayWorkHours = actualWorkHours;
            result.SundayWorkRate = 2.0m; // Double time
        }

        // Public holiday work: Double time (BCEA Section 11)
        if (IsPublicHoliday(checkIn))
        {
            result.PublicHolidayHours = actualWorkHours;
            result.PublicHolidayRate = 2.0m; // Double time
        }

        // Night shift (10pm - 6am): 10% allowance
        if (IsNightShift(checkIn, checkOut))
        {
            result.NightShiftHours = CalculateNightHours(checkIn, checkOut);
            result.NightShiftAllowance = 0.1m; // 10% night allowance
        }

        result.CalculateTotalWage();
        return result;
    }

    /// <summary>
    /// Calculate leave accrual according to BCEA
    /// </summary>
    public LeaveAccrual CalculateLeaveAccrual(
        DateTime employmentStartDate,
        int daysWorkedInCycle,
        string employmentType)
    {
        var result = new LeaveAccrual();
        var monthsWorked = (DateTime.UtcNow - employmentStartDate).TotalDays / 30.44;

        if (employmentType == "Permanent")
        {
            // Annual leave: 21 consecutive days per year (15 working days)
            // Accrual: 1.25 days per month worked
            result.AnnualLeaveDays = Math.Round((decimal)(daysWorkedInCycle * (15m / 365m)), 2);

            // Sick leave: 30 days over 36 months (after 6 months employment)
            if (monthsWorked >= 6 && monthsWorked < 36)
            {
                result.SickLeaveDays = 10; // 1 day for every 26 days worked in first 6 months
            }
            else if (monthsWorked >= 36)
            {
                result.SickLeaveDays = 30; // 30 days per 3-year cycle
            }

            // Family responsibility leave: 3 days per year (after 4 months employment)
            if (monthsWorked >= 4)
            {
                result.FamilyResponsibilityDays = 3;
            }
        }
        else if (employmentType == "FixedTerm" || employmentType == "Casual")
        {
            // Pro-rata annual leave for casual/fixed-term
            result.AnnualLeaveDays = Math.Round((decimal)(daysWorkedInCycle * (15m / 365m)), 2);
        }

        return result;
    }

    /// <summary>
    /// Calculate required break hours based on work duration (BCEA Section 14)
    /// </summary>
    private decimal CalculateRequiredBreakHours(decimal workHours)
    {
        // More than 5 hours: 30 minutes rest
        if (workHours > 5)
            return 0.5m;

        return 0;
    }

    /// <summary>
    /// Check if date is a SA public holiday
    /// </summary>
    private bool IsPublicHoliday(DateTime date)
    {
        return PublicHolidays2026.Any(h => h.Date == date.Date);
    }

    /// <summary>
    /// Check if shift includes night hours (10pm - 6am)
    /// </summary>
    private bool IsNightShift(DateTime checkIn, DateTime checkOut)
    {
        var nightStart = new TimeSpan(22, 0, 0); // 10pm
        var nightEnd = new TimeSpan(6, 0, 0);    // 6am

        return checkIn.TimeOfDay >= nightStart || checkIn.TimeOfDay < nightEnd ||
               checkOut.TimeOfDay > nightStart || checkOut.TimeOfDay <= nightEnd;
    }

    /// <summary>
    /// Calculate total hours worked during night time (10pm - 6am)
    /// </summary>
    private decimal CalculateNightHours(DateTime checkIn, DateTime checkOut)
    {
        var nightStart = new TimeSpan(22, 0, 0); // 10pm
        var nightEnd = new TimeSpan(6, 0, 0);    // 6am
        decimal nightHours = 0;

        var current = checkIn;
        while (current < checkOut)
        {
            if (current.TimeOfDay >= nightStart || current.TimeOfDay < nightEnd)
            {
                nightHours += (decimal)Math.Min((checkOut - current).TotalHours, 1);
            }
            current = current.AddHours(1);
        }

        return nightHours;
    }
}

public class OvertimeCalculation
{
    public decimal TotalHours { get; set; }
    public decimal BreakHours { get; set; }
    public decimal ActualWorkHours { get; set; }
    public decimal ScheduledHours { get; set; }
    public decimal HourlyRate { get; set; }

    public decimal DailyOvertimeHours { get; set; }
    public decimal DailyOvertimeRate { get; set; }

    public decimal SundayWorkHours { get; set; }
    public decimal SundayWorkRate { get; set; }

    public decimal PublicHolidayHours { get; set; }
    public decimal PublicHolidayRate { get; set; }

    public decimal NightShiftHours { get; set; }
    public decimal NightShiftAllowance { get; set; }

    public decimal RegularWage { get; set; }
    public decimal OvertimeWage { get; set; }
    public decimal SundayWage { get; set; }
    public decimal PublicHolidayWage { get; set; }
    public decimal NightShiftBonus { get; set; }
    public decimal TotalWage { get; set; }

    public void CalculateTotalWage()
    {
        // Regular hours (up to scheduled)
        var regularHours = Math.Min(ActualWorkHours, ScheduledHours);
        RegularWage = regularHours * HourlyRate;

        // Overtime wage
        OvertimeWage = DailyOvertimeHours * HourlyRate * DailyOvertimeRate;

        // Sunday wage (replaces regular if worked on Sunday)
        SundayWage = SundayWorkHours > 0 ? SundayWorkHours * HourlyRate * SundayWorkRate : 0;

        // Public holiday wage (replaces regular if worked on public holiday)
        PublicHolidayWage = PublicHolidayHours > 0 ? PublicHolidayHours * HourlyRate * PublicHolidayRate : 0;

        // Night shift bonus (additional to regular wage)
        NightShiftBonus = NightShiftHours * HourlyRate * NightShiftAllowance;

        // Total (Sunday/Public Holiday replaces regular, others are additive)
        if (SundayWorkHours > 0)
            TotalWage = SundayWage + NightShiftBonus;
        else if (PublicHolidayHours > 0)
            TotalWage = PublicHolidayWage + NightShiftBonus;
        else
            TotalWage = RegularWage + OvertimeWage + NightShiftBonus;
    }
}

public class LeaveAccrual
{
    public decimal AnnualLeaveDays { get; set; }
    public decimal SickLeaveDays { get; set; }
    public decimal FamilyResponsibilityDays { get; set; }
}
