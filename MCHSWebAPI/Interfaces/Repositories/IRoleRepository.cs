using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetByNameAsync(string name);
    Task<IEnumerable<Role>> GetAllAsync();
}
