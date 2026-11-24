using ImageEditor.Core.Models;
using ImageEditor.Core.Services;
using Xunit;

namespace ImageEditor.Core.Tests;

public sealed class NameFormatterTests
{
    [Theory]
    [InlineData("CARREN RENATA LEEVANA", 2, true, "Carren Renata")]
    [InlineData("paulus bima", 2, true, "Paulus Bima")]
    [InlineData("Sesilia yovani kurnia puteri saragih", 2, true, "Sesilia Yovani")]
    [InlineData("Elfa Sesilia M.", 3, true, "Elfa Sesilia M.")]
    [InlineData("Maria Thresa t.k", 4, true, "Maria Thresa T.K")]
    [InlineData("  Dinda  ", 2, true, "Dinda")]
    [InlineData("CARREN RENATA LEEVANA", 0, true, "Carren Renata Leevana")]
    [InlineData("CARREN RENATA", 2, false, "CARREN RENATA")]
    [InlineData("", 2, true, "")]
    public void FormatDisplayName_HandlesCasingWordLimitsAndInitials(
        string input, int maxWords, bool titleCase, string expected)
    {
        string result = NameFormatter.FormatDisplayName(input, maxWords, titleCase);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Agnes Sulistiyoningsih", "Agnes Sulistiyoningsih")]
    [InlineData("Fransiska Xaveria A.", "Fransiska Xaveria A")]
    [InlineData("Bad/Name:With*Chars?", "Bad_Name_With_Chars")]
    [InlineData("   ", "unnamed")]
    public void SanitizeFileName_RemovesInvalidCharacters(string input, string expected)
    {
        string result = NameFormatter.SanitizeFileName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildUniqueFileName_AppendsSuffixOnDuplicate()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var record = new AttendeeRecord { Name = "Agnes Sulistiyoningsih" };

        string first = NameFormatter.BuildUniqueFileName(record, "Agnes Sulistiyoningsih", used);
        string second = NameFormatter.BuildUniqueFileName(record, "Agnes Sulistiyoningsih", used);
        string third = NameFormatter.BuildUniqueFileName(record, "Agnes Sulistiyoningsih", used);

        Assert.Equal("Agnes Sulistiyoningsih.png", first);
        Assert.Equal("Agnes Sulistiyoningsih_2.png", second);
        Assert.Equal("Agnes Sulistiyoningsih_3.png", third);
    }
}
