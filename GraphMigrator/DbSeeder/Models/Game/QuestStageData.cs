namespace DbSeeder.Models.Game;

public class QuestStageData
{
    public int Id { get; set; }
    public int QuestId { get; set; }
    public string Name { get; set; }
    public string Objective { get; set; }
    public string Description { get; set; }
}