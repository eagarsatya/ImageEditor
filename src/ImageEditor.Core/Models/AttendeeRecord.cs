namespace ImageEditor.Core.Models;

public sealed class AttendeeRecord
{
    public string Name { get; set; } = string.Empty;
    public string Kelompok { get; set; } = string.Empty;
    public string Kamar { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public int RowNumber { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasKelompok => !string.IsNullOrWhiteSpace(Kelompok);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasKamar => !string.IsNullOrWhiteSpace(Kamar);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsPanitia => !HasKelompok;
}
