namespace DbSeeder.Models.Game;

public class QuestProgressData
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public int QuestId { get; set; }
    public DateTime StartedDate { get; set; }
    public string CurrentStage { get; set; }
}
