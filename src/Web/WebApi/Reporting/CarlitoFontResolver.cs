using PdfSharp.Fonts;

namespace WebApi.Reporting;

/// <summary>Fuente Unicode embebida para que el PDF no dependa de fuentes del servidor.</summary>
public sealed class CarlitoFontResolver : IFontResolver
{
    private const string RegularFace = "Carlito-Regular";
    private const string BoldFace = "Carlito-Bold";

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? BoldFace : RegularFace);

    public byte[]? GetFont(string faceName)
    {
        var resource = "WebApi.Reporting.Fonts." + faceName + ".ttf";
        using var stream = typeof(CarlitoFontResolver).Assembly.GetManifestResourceStream(resource);
        if (stream is null) return null;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
