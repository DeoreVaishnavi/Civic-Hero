namespace CivicHero.Backend.Core.DTOs.Users
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.User;
        public int ReputationPoints { get; set; } = 0;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
    }
}