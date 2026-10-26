namespace Domain.Entities.AppConfig;

public class Configuration
{
    public int ConfigurationId { get; set; }
    public string ConfigurationName { get; set; } = null!;
    public string ConfigurationValue { get; set; } = null!;
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public int EnvironmentId { get; set; }
    public Environment Environment { get; set; } = null!;
    public bool IsActive { get; set; }
}
