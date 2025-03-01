namespace DbSeeder.Models.SocialNetwork;

public class LikeData
{
    public Guid LikeId { get; set; }
    public DateTime CreationDate { get; set; }
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
}
