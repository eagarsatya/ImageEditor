using ImageEditor.Core.Services;
using MiniExcelLibs;
using Xunit;

namespace ImageEditor.Core.Tests;

public sealed class DataLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DataLoader _loader = new();

    public DataLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ImageEditorTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_JsonFile_ParsesAndSkipsBlankNames()
    {
        string path = Path.Combine(_tempDir, "attendees.json");
        File.WriteAllText(path, """
        [
          { "Name": "  Dinda ", "Kelompok": "04", "Kamar": "Cendrawasih-1-111" },
          { "Name": "   ", "Kelompok": "05", "Kamar": "Manyar-1-120" },
          { "Name": "Romo Puspo" }
        ]
        """);

        DataLoadResult result = _loader.Load(path);

        Assert.Equal(2, result.Records.Count);
        Assert.Single(result.Warnings);
        Assert.Equal("Dinda", result.Records[0].Name);
        Assert.Equal("04", result.Records[0].Kelompok);
        Assert.False(result.Records[0].IsPanitia);
        Assert.True(result.Records[1].IsPanitia);
    }

    [Fact]
    public void Load_CsvFile_SupportsQuotedValuesAndIndonesianHeaders()
    {
        string path = Path.Combine(_tempDir, "attendees.csv");
        File.WriteAllText(path, """
        Nama,Kelompok,Kamar
        "Agnes, S.",09,Cendrawasih-1-105
        Valdo,,
        """);

        DataLoadResult result = _loader.Load(path);

        Assert.Equal(2, result.Records.Count);
        Assert.Equal("Agnes, S.", result.Records[0].Name);
        Assert.Equal("09", result.Records[0].Kelompok);
        Assert.Equal("Cendrawasih-1-105", result.Records[0].Kamar);
        Assert.True(result.Records[1].IsPanitia);
    }

    [Fact]
    public void Load_ExcelFile_ParsesRows()
    {
        string path = Path.Combine(_tempDir, "attendees.xlsx");
        var rows = new[]
        {
            new { Name = "Eagar Satya", Kelompok = "14", Kamar = "Kutilang-2-208" },
            new { Name = "Romo Kristi", Kelompok = "", Kamar = "" }
        };
        MiniExcel.SaveAs(path, rows);

        DataLoadResult result = _loader.Load(path);

        Assert.Equal(2, result.Records.Count);
        Assert.Equal("Eagar Satya", result.Records[0].Name);
        Assert.Equal("14", result.Records[0].Kelompok);
        Assert.True(result.Records[1].IsPanitia);
    }
}
