namespace DbSeeder.Models.SocialNetwork;

public class UserData
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime RegistryDate { get; set; }
    public Guid ProfileId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int Gender { get; set; }
    public string PlaceOfStudy { get; set; }
}
