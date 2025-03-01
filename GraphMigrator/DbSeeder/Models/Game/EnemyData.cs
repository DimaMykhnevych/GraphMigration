namespace DbSeeder.Models.Game;

public class EnemyData
{
    public int Id { get; set; }
    public int Level { get; set; }
    public int Reward { get; set; }
    public int HP { get; set; }
    public string Coordinates { get; set; }
    public float SpawnRadius { get; set; }
    public int SpawnLimit { get; set; }
}
