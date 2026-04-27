using MCHSWebAPI.Models;

namespace MCHSWebAPI.Services.RoleService.RoleService;
public interface IRoleService
{
    Task<IEnumerable<Role>> GetAllAsync();
}
