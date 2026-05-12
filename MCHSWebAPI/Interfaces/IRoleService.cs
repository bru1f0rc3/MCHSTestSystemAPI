using MCHSWebAPI.Models;

namespace MCHSWebAPI.Interfaces;

public interface IRoleService
{
    Task<IEnumerable<Role>> GetAllAsync();
}
