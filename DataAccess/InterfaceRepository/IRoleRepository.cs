namespace DataAccess.InterfaceRepository { public interface IRoleRepository { Task<object> GetAllRole(); Task<bool> SoftDeleteAsync(Guid id); } }