namespace DataAccess.InterfaceRepository { public interface IWorkTimeSettingRepository { Task<WorkTimeSetting> CreateAsync(WorkTimeSetting workTimeSetting); Task<List<WorkTimeSetting>> GetAllAsync(); Task<WorkTimeSetting> GetByIdAsync(Guid id); Task<bool> SoftDeleteAsync(Guid id); Task<WorkTimeSetting> UpdateAsync(WorkTimeSetting workTimeSetting); } }