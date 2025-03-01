namespace DbSeeder.Models.SocialNetwork;

public class PostData
{
    public Guid PostId { get; set; }
    public string Content { get; set; }
    public string Description { get; set; }
    public DateTime CreationDate { get; set; }
    public Guid UserId { get; set; }
}

