using Microsoft.AspNetCore.Mvc;
using MCHSProject.Models;
using MCHSProject.Services.Users;
using MCHSProject.DTO.Users;

namespace MCHSProject.Controllers.Users
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDTO dto)
        {
            await _userService.AddUserAsync(dto);
            return Ok(new { message = "User created successfully" });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UserDTO dto)
        {
            await _userService.UpdateUserAsync(dto);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserDTO dto)
        {
            await _userService.DeleteUserAsync(dto);
            return NoContent();
        }
    }
}
