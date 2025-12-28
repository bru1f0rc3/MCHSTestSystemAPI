using Microsoft.AspNetCore.Mvc;
using MCHSProject.Services.Roles;

namespace MCHSProject.Controllers.Roles
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly RoleService _roleService;

        public RolesController(RoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
                return NotFound();
            
            return Ok(role);
        }

        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetRoleByName(string name)
        {
            var role = await _roleService.GetRoleByNameAsync(name);
            if (role == null)
                return NotFound();
            
            return Ok(role);
        }
    }
}
