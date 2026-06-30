namespace CivicHero.Backend.Core.DTOs.Users
{
    public class UserDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int ReputationPoints { get; set; }

        public bool IsActive { get; set; }
    }
}