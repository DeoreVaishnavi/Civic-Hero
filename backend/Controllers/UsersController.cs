using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _userRepository;

        public UsersController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> getUser()
        {
            var users = await _userRepository.GetAllAsync();

            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                ReputationPoints = user.ReputationPoints,
                Email = user.Email,
                IsActive = user.IsActive,
            }).ToList();
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> getUserById(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
                return NotFound();

            var userDtos = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                ReputationPoints = user.ReputationPoints,
                Email = user.Email,
                IsActive = user.IsActive,
            };
            return Ok(userDtos);
        }

        //Post Method
        [HttpPost]
        public async Task<IActionResult> createUser(CreateUserDto createuserDto)
        {
            var emailExists = await _userRepository.EmailExist(createuserDto.Email);

            if (emailExists)
                return BadRequest("Email Already Exsist");

            var user = new User
            {
                FullName = createuserDto.FullName,
                Email = createuserDto.Email,
                PhoneNumber = createuserDto.PhoneNumber,
                Role = createuserDto.Role.ToString(),
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
                Role = createdUser.Role.ToString(),
                ReputationPoints = createdUser.ReputationPoints,
                Email = createdUser.Email,
                IsActive = createdUser.IsActive,
            };

            return CreatedAtAction(nameof(getUserById), new { id = userDtos.Id }, userDtos);
        }

        //Put Method 
        [HttpPut("{id}")]
        public async Task<IActionResult> updateUser(int id, UpdateUserDto updateuserDto)
        {
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
                Role = updatedUser.Role.ToString(),
                ReputationPoints = updatedUser.ReputationPoints,
                IsActive = updatedUser.IsActive
            };
            return Ok(userDto);
        }

        [HttpDelete("{id}")]
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
