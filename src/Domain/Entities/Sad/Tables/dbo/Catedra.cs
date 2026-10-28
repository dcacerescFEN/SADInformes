namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Read projection of the existing table; canonical mapping: MappingDB/Models/Catedra.cs.</summary>
public sealed class Catedra
{
    public string CodCatedra { get; set; } = null!;
    public string? NomCatedra { get; set; }
}
