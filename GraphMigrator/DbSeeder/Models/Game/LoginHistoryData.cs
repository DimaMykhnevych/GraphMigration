namespace DbSeeder.Models.Game;

public class LoginHistoryData
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime LoginDateTime { get; set; }
    public DateTime LogoutDateTime { get; set; }
    public string IpAddress { get; set; }
    public string DeviceName { get; set; }
}
