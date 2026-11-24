using System.Text;
using System.Text.Json;
using ImageEditor.Core.Models;
using MiniExcelLibs;

namespace ImageEditor.Core.Services;

public sealed record DataLoadResult(
    List<AttendeeRecord> Records,
    List<string> Warnings);

public sealed class DataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".json", ".csv", ".xlsx" };

    public DataLoadResult Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Data file path cannot be empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Data file not found: {filePath}", filePath);
        }

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".json" => LoadJson(filePath),
            ".csv" => LoadCsv(filePath),
            ".xlsx" => LoadExcel(filePath),
            _ => throw new NotSupportedException(
                $"Unsupported file format '{ext}'. Supported formats: {string.Join(", ", SupportedExtensions)}")
        };
    }

    private static DataLoadResult LoadJson(string filePath)
    {
        string json = File.ReadAllText(filePath);
        var raw = JsonSerializer.Deserialize<List<AttendeeRecord>>(json, JsonOptions) ?? new List<AttendeeRecord>();
        return NormalizeRecords(raw);
    }

    private static DataLoadResult LoadCsv(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        var records = new List<AttendeeRecord>();
        var warnings = new List<string>();

        if (lines.Length == 0)
        {
            warnings.Add("CSV file is empty.");
            return new DataLoadResult(records, warnings);
        }

        List<string> headers = ParseCsvLine(lines[0]);
        int nameIdx = FindColumnIndex(headers, "name", "nama", "fullname", "full name");
        int kelompokIdx = FindColumnIndex(headers, "kelompok", "group", "kel");
        int kamarIdx = FindColumnIndex(headers, "kamar", "room");

        if (nameIdx < 0)
        {
            throw new InvalidDataException(
                "CSV header must include a 'Name' (or 'Nama') column.");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            List<string> cols = ParseCsvLine(lines[i]);
            string name = GetColumnValue(cols, nameIdx);
            string kelompok = GetColumnValue(cols, kelompokIdx);
            string kamar = GetColumnValue(cols, kamarIdx);

            records.Add(new AttendeeRecord
            {
                Name = name,
                Kelompok = kelompok,
                Kamar = kamar,
                RowNumber = i + 1
            });
        }

        var normalized = NormalizeRecords(records);
        normalized.Warnings.InsertRange(0, warnings);
        return normalized;
    }

    private static DataLoadResult LoadExcel(string filePath)
    {
        var rows = MiniExcel.Query(filePath, useHeaderRow: true).ToList();
        var records = new List<AttendeeRecord>();
        int rowIndex = 2;

        foreach (var rowObj in rows)
        {
            if (rowObj is not IDictionary<string, object> dict)
            {
                rowIndex++;
                continue;
            }

            string name = GetDictionaryValue(dict, "name", "nama", "fullname", "full name");
            string kelompok = GetDictionaryValue(dict, "kelompok", "group", "kel");
            string kamar = GetDictionaryValue(dict, "kamar", "room");

            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(kelompok) &&
                string.IsNullOrWhiteSpace(kamar))
            {
                rowIndex++;
                continue;
            }

            records.Add(new AttendeeRecord
            {
                Name = name,
                Kelompok = kelompok,
                Kamar = kamar,
                RowNumber = rowIndex
            });

            rowIndex++;
        }

        return NormalizeRecords(records);
    }

    private static DataLoadResult NormalizeRecords(List<AttendeeRecord> raw)
    {
        var valid = new List<AttendeeRecord>(raw.Count);
        var warnings = new List<string>();

        for (int i = 0; i < raw.Count; i++)
        {
            var item = raw[i];
            int rowNum = item.RowNumber > 0 ? item.RowNumber : i + 1;

            item.Name = (item.Name ?? string.Empty).Trim();
            item.Kelompok = (item.Kelompok ?? string.Empty).Trim();
            item.Kamar = (item.Kamar ?? string.Empty).Trim();
            item.RowNumber = rowNum;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                warnings.Add($"Row {rowNum}: skipped because Name is empty.");
                continue;
            }

            valid.Add(item);
        }

        return new DataLoadResult(valid, warnings);
    }

    private static int FindColumnIndex(List<string> headers, params string[] candidates)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            string h = headers[i].Trim().TrimStart('\uFEFF').ToLowerInvariant();
            if (candidates.Contains(h))
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetColumnValue(List<string> cols, int index)
    {
        if (index < 0 || index >= cols.Count)
        {
            return string.Empty;
        }

        return cols[index].Trim();
    }

    private static string GetDictionaryValue(IDictionary<string, object> dict, params string[] candidates)
    {
        foreach (var kvp in dict)
        {
            string key = kvp.Key.Trim().TrimStart('\uFEFF').ToLowerInvariant();
            if (candidates.Contains(key))
            {
                return kvp.Value?.ToString()?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    public static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        result.Add(current.ToString());
        return result;
    }
}
