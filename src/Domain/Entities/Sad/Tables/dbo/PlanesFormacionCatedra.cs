namespace Domain.Entities.Sad.Tables.dbo;

/// <summary>Read projection of the existing table; canonical mapping: MappingDB/Models/PlanesFormacionCatedra.cs.</summary>
public sealed class PlanesFormacionCatedra
{
    public string Decreto { get; set; } = null!;
    public string CodPlanFormacion { get; set; } = null!;
    public string CodCatedra { get; set; } = null!;
    public string TipoCatedra { get; set; } = null!;
}
