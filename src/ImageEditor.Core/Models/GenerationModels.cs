namespace ImageEditor.Core.Models;

public sealed class GenerationOptions
{
    public string? NameFilter { get; set; }
    public string? KelompokFilter { get; set; }
    public int? Limit { get; set; }
    public bool DryRun { get; set; }
    public string? OutputDirectoryOverride { get; set; }
    public string? TemplateImagePathOverride { get; set; }
}

public sealed class RecordGenerationResult
{
    public required AttendeeRecord Record { get; init; }
    public bool Success { get; init; }
    public string FormattedName { get; init; } = string.Empty;
    public string? OutputPath { get; init; }
    public List<string> Warnings { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

public sealed class GenerationProgress
{
    public int Completed { get; init; }
    public int Total { get; init; }
    public string CurrentName { get; init; } = string.Empty;
}

public sealed class GenerationSummary
{
    public int TotalLoaded { get; init; }
    public int TotalProcessed { get; init; }
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public int WarningCount { get; init; }
    public TimeSpan Duration { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public bool DryRun { get; init; }
    public List<RecordGenerationResult> Results { get; init; } = new();
}
