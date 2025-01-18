using DataAccess.InterfaceRepository;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccess.Repository
{
    public class RiskPerformanceSettingRepository : Repository<RiskPerformanceSetting>, IRiskPerformanceSettingRepository
    {
        private readonly MyDbContext _dbContext;

        public RiskPerformanceSettingRepository(MyDbContext context) : base(context)
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

        // Create a new RiskPerformanceSetting record with LateMinuteAllow and EarlyLeaveMinuteAllow
        public async Task<object> CreateRiskPerformanceSettingAsync(int lateMinuteAllow, int earlyLeaveMinuteAllow)
        {
            var idd = Guid.NewGuid();
            var riskPerformanceSetting = new RiskPerformanceSetting
            {
                Id = idd,
                Hours = lateMinuteAllow,
                Days = earlyLeaveMinuteAllow,
                DateSet = DateTime.UtcNow, // Automatically set the date
                IsDeleted = false
            };
            await _dbContext.RiskPerformanceSettings.AddAsync(riskPerformanceSetting);
            if (await _dbContext.SaveChangesAsync() <= 0) return "Fail to Create new Setting";

            return idd;
        }

        // Get a RiskPerformanceSetting by ID, returning only LateMinuteAllow and EarlyLeaveMinuteAllow
        public async Task<object> GetRiskPerformanceSettingByIdAsync(Guid id)
        {
            var riskPerformanceSetting = await _dbContext.RiskPerformanceSettings
                .Where(r => !r.IsDeleted && r.Id == id)
                .Select(r => new
                {
                    LateMinuteAllow = r.Hours,
                    EarlyLeaveMinuteAllow = r.Days,
                    CreatedDate = r.DateSet
                })
                .FirstOrDefaultAsync();

            return riskPerformanceSetting;
        }

        // Update LateMinuteAllow and EarlyLeaveMinuteAllow for a specific record
        public async Task<bool> UpdateRiskPerformanceSettingAsync(Guid id, int lateMinuteAllow, int earlyLeaveMinuteAllow)
        {
            var riskPerformanceSetting = await _dbContext.RiskPerformanceSettings.FindAsync(id);
            if (riskPerformanceSetting == null || riskPerformanceSetting.IsDeleted) return false;

            riskPerformanceSetting.Hours = lateMinuteAllow;
            riskPerformanceSetting.Days = earlyLeaveMinuteAllow;

            _dbContext.RiskPerformanceSettings.Update(riskPerformanceSetting);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        // Get all RiskPerformanceSettings (active only), returning only LateMinuteAllow and EarlyLeaveMinuteAllow
        public async Task<List<object>> GetAllRiskPerformanceSettingsAsync()
        {
            return await _dbContext.RiskPerformanceSettings
                .Where(r => !r.IsDeleted)
                .Select(r => (object)new
                {
                    r.Id,
                    LateMinuteAllow = r.Hours,
                    EarlyLeaveMinuteAllow = r.Days,
                    CreatedDate = r.DateSet
                })
                .ToListAsync();
        }

        // Soft delete a RiskPerformanceSetting by its ID
        public async Task<bool> SoftDeleteRiskPerformanceSettingAsync(Guid id)
        {
            var riskPerformanceSetting = await _dbContext.RiskPerformanceSettings.FindAsync(id);
            if (riskPerformanceSetting == null || riskPerformanceSetting.IsDeleted) return false;

            riskPerformanceSetting.IsDeleted = true;
            return await _dbContext.SaveChangesAsync() > 0;
        }
    }
}
