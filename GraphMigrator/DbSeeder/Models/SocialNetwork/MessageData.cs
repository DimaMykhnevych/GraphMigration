namespace DbSeeder.Models.SocialNetwork;

public class MessageData
{
    public Guid MessageId { get; set; }
    public string Content { get; set; }
    public DateTime SendingDate { get; set; }
    public bool IsRead { get; set; }
}
