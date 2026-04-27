using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MCHSWebAPI.DTOs;
using MCHSWebAPI.Services.UserService.UserService;

namespace MCHSWebAPI.Controllers.UserController;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
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
        var result = await _userService.CreateAsync(request);
        if (result == null)
            return BadRequest(ApiResponse<UserDto>.Fail("Не удалось создать пользователя. Возможно, имя уже занято."));

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<UserDto>.Ok(result, "Пользователь создан"));
    }
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(id, request);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Пользователь не найден или данные некорректны"));

        return Ok(ApiResponse<bool>.Ok(true, "Пользователь обновлен"));
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Пользователь не найден"));

        return Ok(ApiResponse<bool>.Ok(true, "Пользователь удален"));
    }
}
