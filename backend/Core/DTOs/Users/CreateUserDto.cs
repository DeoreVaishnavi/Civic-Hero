using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Users
{
    public class CreateUserDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public Role Role { get; set; } = Role.User;
    }
}