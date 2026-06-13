using Microsoft.AspNetCore.Mvc;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;
using MCHSWebAPI.Models;

namespace MCHSWebAPI.Controllers.UserController;

/// <summary>
/// Контроллер для работы с ролями пользователей
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    /// <summary>
    /// Создаёт контроллер и получает сервис ролей
    /// </summary>
    /// <param name="roleService">Сервис для работы с ролями</param>
    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Возвращает список всех ролей
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Role>>>> GetAll()
    {
        var result = await _roleService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<Role>>.Ok(result));
    }
}
