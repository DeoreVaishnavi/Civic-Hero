namespace CivicHero.Backend.Core.DTOs.Users
{
    public class UpdateUserDto
    {

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
