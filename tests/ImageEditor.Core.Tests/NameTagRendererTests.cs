using ImageEditor.Core.Models;
using ImageEditor.Core.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ImageEditor.Core.Tests;

public sealed class NameTagRendererTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _templatePath;
    private readonly NameTagRenderer _renderer = new();

    public NameTagRendererTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ImageEditorRender_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _templatePath = Path.Combine(_tempDir, "template.png");
        using var img = new Image<Rgba32>(1000, 1400, Color.White);
        img.SaveAsPng(_templatePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_CreatesPngsAndAutoShrinksLongNames()
    {
        var records = new List<AttendeeRecord>
        {
            new() { Name = "Agnes Sulistiyoningsih", Kelompok = "09", Kamar = "Cendrawasih-1-105", RowNumber = 1 },
            new() { Name = "VincentiusLorenzoberchmanGunawan", Kelompok = "12", Kamar = "Jalak Bali-2-213", RowNumber = 2 },
            new() { Name = "Romo Puspo", Kelompok = "", Kamar = "", RowNumber = 3 }
        };

        var config = new TemplateConfig
        {
            TemplateImagePath = _templatePath,
            OutputDirectory = Path.Combine(_tempDir, "out"),
            OrganizeByKelompok = true
        };

        GenerationSummary summary = _renderer.Generate(records, config, _tempDir);

        Assert.Equal(3, summary.Succeeded);
        Assert.Equal(0, summary.Failed);
        Assert.True(File.Exists(Path.Combine(_tempDir, "out", "Kel-09", "Agnes Sulistiyoningsih.png")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "out", "Kel-12", "Vincentiuslorenzoberchmangunawan.png")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "out", "Panitia", "Romo Puspo.png")));

        // Verify the very long single-word name triggered auto-shrink warning
        RecordGenerationResult longResult = summary.Results[1];
        Assert.Contains(longResult.Warnings, w => w.Contains("auto-shrunk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Generate_DryRun_DoesNotWriteFiles()
    {
        var records = new List<AttendeeRecord>
        {
            new() { Name = "Dinda", Kelompok = "04", Kamar = "Cendrawasih-1-111", RowNumber = 1 }
        };

        string outDir = Path.Combine(_tempDir, "dry_out");
        var config = new TemplateConfig
        {
            TemplateImagePath = _templatePath,
            OutputDirectory = outDir
        };

        GenerationSummary summary = _renderer.Generate(
            records,
            config,
            _tempDir,
            new GenerationOptions { DryRun = true });

        Assert.True(summary.DryRun);
        Assert.Equal(1, summary.Succeeded);
        Assert.False(Directory.Exists(outDir));
    }
}
