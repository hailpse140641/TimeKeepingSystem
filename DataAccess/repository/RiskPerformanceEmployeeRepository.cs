using DataAccess.InterfaceRepository;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repository
{
    public class RiskPerformanceEmployeeRepository : Repository<RiskPerformanceEmployee>, IRiskPerformanceEmployeeRepository
    {
        private readonly MyDbContext _dbContext;

        public RiskPerformanceEmployeeRepository(MyDbContext context) : base(context)
        {
            // You can add more specific methods here if needed
            _dbContext = context;
        }



        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            try
            {
                await base.SoftDeleteAsync(id);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public async Task<object> GetEmployeesViolatingRiskSettingForMonth(int month, int year)
        {
            // Step 1: Get the latest active RiskPerformanceSetting
            var latestRiskSetting = await _dbContext.RiskPerformanceSettings
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.DateSet)
                .FirstOrDefaultAsync();

            if (latestRiskSetting == null)
            {
                return "No active RiskPerformanceSettings found.";
            }

            int lateMinuteAllow = latestRiskSetting.Hours; // Allowable late minutes before violation
            int earlyLeaveMinuteAllow = latestRiskSetting.Days; // Allowable early leave minutes before violation

            // Step 2: Get all WorkslotEmployees within the selected month and year
            var workslotEmployees = await _dbContext.WorkslotEmployees
                .Include(we => we.Employee).ThenInclude(e => e.Department) // Include Employee details
                .Include(we => we.Workslot) // Include Workslot details
                .Where(we => !we.IsDeleted &&
                             we.Workslot.DateOfSlot.Month == month &&
                             we.Workslot.DateOfSlot.Year == year)
                .ToListAsync();

            // Step 3: Identify violations based on LateMinuteAllow and EarlyLeaveMinuteAllow
            var employeeViolations = new Dictionary<Guid, List<(DateTime date, int lateMinutes, int earlyLeaveMinutes, string checkInTime, string checkOutTime, string expectCheckIn, string expectCheckOut, bool isMorning, string teamName, string teamId)>>();

            foreach (var we in workslotEmployees)
            {
                int lateMinutes = 0, earlyLeaveMinutes = 0;
                string checkInTime = we.CheckInTime ?? "N/A";
                string checkOutTime = we.CheckOutTime ?? "N/A";
                string expectCheckIn = we.Workslot.FromHour ?? "N/A";
                string expectCheckOut = we.Workslot.ToHour ?? "N/A";
                bool isMorning = we.Workslot.IsMorning;
                string teamName = we.Employee?.Department?.Name ?? "N/A";
                string teamId = we.Employee?.Department?.Id.ToString() ?? "N/A";

                if (!string.IsNullOrEmpty(we.CheckInTime) && !string.IsNullOrEmpty(we.Workslot.FromHour))
                {
                    lateMinutes = (int)(TimeSpan.Parse(we.CheckInTime) - TimeSpan.Parse(we.Workslot.FromHour)).TotalMinutes;
                }

                if (!string.IsNullOrEmpty(we.CheckOutTime) && !string.IsNullOrEmpty(we.Workslot.ToHour))
                {
                    earlyLeaveMinutes = (int)(TimeSpan.Parse(we.Workslot.ToHour) - TimeSpan.Parse(we.CheckOutTime)).TotalMinutes;
                }

                if (lateMinutes > lateMinuteAllow || earlyLeaveMinutes > earlyLeaveMinuteAllow)
                {
                    if (!employeeViolations.ContainsKey(we.EmployeeId))
                    {
                        employeeViolations[we.EmployeeId] = new List<(DateTime, int, int, string, string, string ,string, bool, string, string)>();
                    }

                    employeeViolations[we.EmployeeId].Add((we.Workslot.DateOfSlot, lateMinutes, earlyLeaveMinutes, checkInTime, checkOutTime, expectCheckIn, expectCheckOut, isMorning, teamName, teamId));
                }
            }

            return new
            {
                SelectedMonth = month,
                SelectedYear = year,
                LatestRiskSetting = new
                {
                    latestRiskSetting.Id,
                    LateMinutesAllow = latestRiskSetting.Hours,
                    EarlyLeaveMinutesAllow = latestRiskSetting.Days,
                    latestRiskSetting.DateSet
                },
                ViolatingEmployees = employeeViolations.Select(ev => new
                {
                    EmployeeId = ev.Key,
                    EmployeeName = workslotEmployees
                        .Where(we => we.EmployeeId == ev.Key)
                        .Select(we => we.Employee.FirstName + " " + we.Employee.LastName)
                        .FirstOrDefault(),
                    TeamId = ev.Value.FirstOrDefault().teamId, 
                    TeamName = ev.Value.FirstOrDefault().teamName, 
                    Violations = ev.Value.Select(v => new
                    {
                        Date = v.date.ToString("dd-MM-yyyy"),
                        LateMinutes = v.lateMinutes > lateMinuteAllow ? v.lateMinutes : 0,
                        EarlyLeaveMinutes = v.earlyLeaveMinutes > earlyLeaveMinuteAllow ? v.earlyLeaveMinutes : 0,
                        CheckInTime = v.isMorning ? v.checkInTime : "N/A",
                        ExpectCheckIn = v.isMorning ? v.expectCheckIn : "N/A",
                        CheckOutTime = v.isMorning ? "N/A" : v.checkOutTime,
                        ExpectCheckOut = v.isMorning ? "N/A" : v.expectCheckOut
                    }).ToList()
                })
            };
        }
    }
}
