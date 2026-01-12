using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.Interfaces.Repositories;
using MCHSWebAPI.DTOs.Common;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;

    public RolesController(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    /// <summary>
    /// Получить список всех ролей
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Role>>>> GetAll()
    {
        var result = await _roleRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<Role>>.Ok(result));
    }
}
