using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: api/users
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> getUser()
        {
            var users = await _userRepository.GetAllAsync();

            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                ReputationPoints = user.ReputationPoints,
                Email = user.Email,
                IsActive = user.IsActive,
            }).ToList();
            return Ok(userDtos);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> getUserById(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            // Users can view their own profile, admins can view any
            var currentUser = await _userRepository.GetByIdAsync(userId);
            if (currentUser == null)
                return Unauthorized();

            if (id != userId && currentUser.Role != Role.Admin)
                return Forbid(); // Not authorized to view other users

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var userDtos = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                ReputationPoints = user.ReputationPoints,
                Email = user.Email,
                IsActive = user.IsActive,
            };
            return Ok(userDtos);
        }

        // POST: api/users
        [HttpPost]
        [AllowAnonymous] // Allow registration without authentication; adjust if needed
        public async Task<IActionResult> createUser(CreateUserDto createuserDto)
        {
            var emailExists = await _userRepository.EmailExist(createuserDto.Email);

            if (emailExists)
                return BadRequest("Email Already Exists");

            var user = new User
            {
                FullName = createuserDto.FullName,
                Email = createuserDto.Email,
                PhoneNumber = createuserDto.PhoneNumber,
                Role = createuserDto.Role,
                // Temporary until Part 2 password hashing is implemented
                PasswordHash = createuserDto.Password,
                ReputationPoints = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            var createdUser = await _userRepository.AddAsync(user);

            var userDtos = new UserDto
            {
                Id = createdUser.Id,
                FullName = createdUser.FullName,
                PhoneNumber = createdUser.PhoneNumber,
                Role = createdUser.Role,
                ReputationPoints = createdUser.ReputationPoints,
                Email = createdUser.Email,
                IsActive = createdUser.IsActive,
            };

            return CreatedAtAction(nameof(getUserById), new { id = userDtos.Id }, userDtos);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> updateUser(int id, UpdateUserDto updateuserDto)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var currentUser = await _userRepository.GetByIdAsync(userId);
            if (currentUser == null)
                return Unauthorized();

            // Users can update their own profile, admins can update any
            if (id != userId && currentUser.Role != Role.Admin)
                return Forbid();

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            user.FullName = updateuserDto.FullName;
            user.PhoneNumber = updateuserDto.PhoneNumber;
            user.IsActive = updateuserDto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            var updatedUser = await _userRepository.UpdateAsync(user);

            var userDto = new UserDto
            {
                Id = updatedUser.Id,
                FullName = updatedUser.FullName,
                Email = updatedUser.Email,
                PhoneNumber = updatedUser.PhoneNumber,
                Role = updatedUser.Role,
                ReputationPoints = updatedUser.ReputationPoints,
                IsActive = updatedUser.IsActive
            };
            return Ok(userDto);
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> deleteUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userRepository.DeleteAsync(user);
            return NoContent();
        }
    }
}