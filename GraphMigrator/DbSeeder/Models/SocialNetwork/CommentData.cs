namespace DbSeeder.Models.SocialNetwork;

public class CommentData
{
    public Guid CommentId { get; set; }
    public string Content { get; set; }
    public DateTime CreationDate { get; set; }
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
}

