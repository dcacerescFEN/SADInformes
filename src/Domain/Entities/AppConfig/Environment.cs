namespace Domain.Entities.AppConfig;

public class Environment
{
    public int EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = null!;
    public bool IsActive { get; set; }
    public ICollection<Configuration> Configurations { get; set; } = [];
}
