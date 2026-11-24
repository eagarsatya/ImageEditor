namespace ImageEditor.Core.Models;

public sealed class TemplateConfig
{
    public string Name { get; set; } = "PLB Nametag Default";
    public string TemplateImagePath { get; set; } = "BaseImage/PLB Nametag.png";
    public string OutputDirectory { get; set; } = "BaseImage/Result";

    public string FontFamily { get; set; } = "Segoe UI";
    public string? FontFilePath { get; set; }
    public float FontSize { get; set; } = 75f;
    public float MinFontSize { get; set; } = 32f;
    public bool FontBold { get; set; } = true;
    public string TextColor { get; set; } = "#000000";

    public float MaxTextWidthRatio { get; set; } = 0.85f;
    public int NameMaxWords { get; set; } = 2;
    public bool ApplyTitleCase { get; set; } = true;

    public string KelompokPrefix { get; set; } = "Kel - ";
    public bool ShowKamarWhenNoKelompok { get; set; } = false;
    public bool OrganizeByKelompok { get; set; } = false;
    public bool OverwriteExisting { get; set; } = true;

    public LayoutConfig Layout { get; set; } = new();
}

public sealed class LayoutConfig
{
    public float XRatio { get; set; } = 0.5f;
    public float NameY { get; set; } = 800f;
    public float NameOnlyY { get; set; } = 925f;
    public float KelompokY { get; set; } = 925f;
    public float KamarY { get; set; } = 1050f;
}
