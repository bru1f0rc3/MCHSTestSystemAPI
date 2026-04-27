using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Models;
using MCHSWebAPI.Services.RoleService.RoleService;

namespace MCHSWebAPI.Controllers.UserController;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Role>>>> GetAll()
    {
        var result = await _roleService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<Role>>.Ok(result));
    }
}
