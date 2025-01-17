using DataAccess.InterfaceRepository;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

            int requiredViolationDays = latestRiskSetting.Days; // The number of days threshold for violation

            // Step 2: Get all WorkslotEmployees within the selected month and year
            var violatingEmployees = await _dbContext.WorkslotEmployees
                .Include(we => we.Employee)
                .Include(we => we.Workslot)
                .Where(we => !we.IsDeleted &&
                             we.Workslot.DateOfSlot.Month == month &&
                             we.Workslot.DateOfSlot.Year == year)
                .ToListAsync();

            // Step 3: Identify violations based on check-in and check-out times
            var employeeViolations = new Dictionary<Guid, List<(DateTime date, bool isLate, bool isEarlyLeave, string checkIn, string expectedStart, string checkOut, string expectedEnd)>>();

            foreach (var we in violatingEmployees)
            {
                bool isLate = false, isEarlyLeave = false;

                if (!string.IsNullOrEmpty(we.CheckInTime) && !string.IsNullOrEmpty(we.Workslot.FromHour))
                {
                    isLate = TimeSpan.Parse(we.CheckInTime) > TimeSpan.Parse(we.Workslot.FromHour);
                }

                if (!string.IsNullOrEmpty(we.CheckOutTime) && !string.IsNullOrEmpty(we.Workslot.ToHour))
                {
                    isEarlyLeave = TimeSpan.Parse(we.CheckOutTime) < TimeSpan.Parse(we.Workslot.ToHour);
                }

                if (isLate || isEarlyLeave)
                {
                    if (!employeeViolations.ContainsKey(we.EmployeeId))
                    {
                        employeeViolations[we.EmployeeId] = new List<(DateTime, bool, bool, string, string, string, string)>();
                    }

                    employeeViolations[we.EmployeeId].Add((
                        we.Workslot.DateOfSlot,
                        isLate,
                        isEarlyLeave,
                        we.CheckInTime,
                        we.Workslot.FromHour,
                        we.CheckOutTime,
                        we.Workslot.ToHour
                    ));
                }
            }

            // Step 4: Filter employees who exceed the violation threshold
            var result = new List<object>();

            foreach (var entry in employeeViolations)
            {
                if (entry.Value.Count >= requiredViolationDays)
                {
                    var violationDetails = entry.Value.Select(v => new
                    {
                        date = v.date.ToString("yyyy-MM-dd"),
                        isLate = v.isLate,
                        isEarlyLeave = v.isEarlyLeave,
                        checkIn = v.isEarlyLeave ? "" : v.checkIn,
                        expectedStart = v.isEarlyLeave ? "" : v.expectedStart,
                        checkOut = v.isLate ? "" : v.checkOut,
                        expectedEnd = v.isLate ? "" : v.expectedEnd
                    }).ToList();
                    var employee = _dbContext.Employees.Include(e => e.Department).Where(e => e.Id == entry.Key).FirstOrDefault();
                    result.Add(new
                    {
                        EmployeeId = entry.Key,
                        EmployeeName = employee?.FirstName + " " + employee?.LastName,
                        Team = employee?.Department?.Name,
                        ViolationCount = entry.Value.Count,
                        ViolationJSON = new
                        {
                            violationType = "Attendance Issues",
                            count = entry.Value.Count,
                            timestamps = violationDetails
                        }
                    });
                }
            }

            return new
            {
                SelectedMonth = month,
                SelectedYear = year,
                LatestRiskSetting = new
                {
                    latestRiskSetting.Id,
                    latestRiskSetting.Hours,
                    latestRiskSetting.Days,
                    latestRiskSetting.DateSet
                },
                ViolatingEmployees = result
            };
        }

    }
}
