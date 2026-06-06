using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Interfaces;

namespace MCHSWebAPI.Controllers.UserController;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "superadmin")]
public class UsersController : AuthorizedControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private bool IsSuperAdmin => User.IsInRole("superadmin");

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResponse<UserDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<UserDto>.Fail("Пользователь не найден"));

        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            var result = await _userService.CreateAsync(request, IsSuperAdmin);
            if (result == null)
                return BadRequest(ApiResponse<UserDto>.Fail("Не удалось создать пользователя. Возможно, имя уже занято."));

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<UserDto>.Ok(result, "Пользователь создан"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (id == GetUserId())
            return BadRequest(ApiResponse<bool>.Fail(
                "Нельзя редактировать собственный аккаунт через панель администратора. Используйте «Личные данные» в профиле."));

        try
        {
            var result = await _userService.UpdateAsync(id, request, IsSuperAdmin);
            if (!result)
                return NotFound(ApiResponse<bool>.Fail("Пользователь не найден или нет полей для обновления"));

            return Ok(ApiResponse<bool>.Ok(true, "Пользователь обновлен"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        if (id == GetUserId())
            return BadRequest(ApiResponse<bool>.Fail("Нельзя удалить собственный аккаунт через панель администратора"));

        try
        {
            var result = await _userService.DeleteAsync(id, IsSuperAdmin);
            if (!result)
                return NotFound(ApiResponse<bool>.Fail("Пользователь не найден"));

            return Ok(ApiResponse<bool>.Ok(true, "Пользователь удален"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }
}
