using System.ComponentModel;
using ImageEditor.Cli.Tui;
using ImageEditor.Core.Models;
using ImageEditor.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ImageEditor.Cli.Commands;

public sealed class GenerateCommand : Command<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-d|--data <FILE>")]
        [Description("Path to attendee data file (.json, .csv, or .xlsx).")]
        public string DataPath { get; init; } = string.Empty;

        [CommandOption("-c|--config <FILE>")]
        [Description("Path to nametag.config.json (optional).")]
        public string? ConfigPath { get; init; }

        [CommandOption("-t|--template <FILE>")]
        [Description("Override template image path.")]
        public string? TemplatePath { get; init; }

        [CommandOption("-o|--output <DIR>")]
        [Description("Override output directory.")]
        public string? OutputDirectory { get; init; }

        [CommandOption("-f|--filter <TEXT>")]
        [Description("Only generate nametags whose name contains this text.")]
        public string? NameFilter { get; init; }

        [CommandOption("-k|--kelompok <GROUP>")]
        [Description("Only generate nametags for a specific Kelompok (or 'Panitia').")]
        public string? KelompokFilter { get; init; }

        [CommandOption("-l|--limit <COUNT>")]
        [Description("Limit generation to the first N matching attendees (useful for previews).")]
        public int? Limit { get; init; }

        [CommandOption("--dry-run")]
        [Description("Validate inputs and layout sizing without writing PNG files.")]
        public bool DryRun { get; init; }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DataPath))
        {
            return ValidationResult.Error("Please specify --data <file>.");
        }

        return ValidationResult.Success();
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        string workspaceRoot = ConfigService.ResolveWorkspaceRoot();
        var configService = new ConfigService();
        string configPath = string.IsNullOrWhiteSpace(settings.ConfigPath)
            ? Path.Combine(workspaceRoot, ConfigService.DefaultConfigFileName)
            : ConfigService.ResolvePath(settings.ConfigPath, workspaceRoot);

        TemplateConfig config = configService.LoadOrCreateDefault(configPath);
        string resolvedDataPath = ConfigService.ResolvePath(settings.DataPath, workspaceRoot);

        var loader = new DataLoader();
        DataLoadResult data = loader.Load(resolvedDataPath);

        if (data.Warnings.Count > 0)
        {
            foreach (string warning in data.Warnings)
            {
                AnsiConsole.MarkupLine($"[yellow]Load warning:[/] {Markup.Escape(warning)}");
            }
        }

        var options = new GenerationOptions
        {
            NameFilter = settings.NameFilter,
            KelompokFilter = settings.KelompokFilter,
            Limit = settings.Limit,
            DryRun = settings.DryRun,
            OutputDirectoryOverride = settings.OutputDirectory,
            TemplateImagePathOverride = settings.TemplatePath
        };

        var renderer = new NameTagRenderer();
        GenerationSummary summary = ConsoleUiHelpers.RunWithProgress(
            renderer,
            data.Records,
            config,
            workspaceRoot,
            options);

        AnsiConsole.WriteLine();
        ConsoleUiHelpers.RenderSummary(summary);
        return summary.Failed > 0 ? 1 : 0;
    }
}

public sealed class InspectCommand : Command<InspectCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-d|--data <FILE>")]
        [Description("Path to attendee data file (.json, .csv, or .xlsx).")]
        public string DataPath { get; init; } = string.Empty;

        [CommandOption("-c|--config <FILE>")]
        [Description("Path to nametag.config.json (optional).")]
        public string? ConfigPath { get; init; }

        [CommandOption("-n|--rows <COUNT>")]
        [Description("Number of preview rows to display (default: 15).")]
        [DefaultValue(15)]
        public int PreviewRows { get; init; } = 15;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DataPath))
        {
            return ValidationResult.Error("Please specify --data <file>.");
        }

        return ValidationResult.Success();
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        string workspaceRoot = ConfigService.ResolveWorkspaceRoot();
        var configService = new ConfigService();
        string configPath = string.IsNullOrWhiteSpace(settings.ConfigPath)
            ? Path.Combine(workspaceRoot, ConfigService.DefaultConfigFileName)
            : ConfigService.ResolvePath(settings.ConfigPath, workspaceRoot);

        TemplateConfig config = configService.LoadOrCreateDefault(configPath);
        string resolvedDataPath = ConfigService.ResolvePath(settings.DataPath, workspaceRoot);

        var loader = new DataLoader();
        DataLoadResult data = loader.Load(resolvedDataPath);
        ConsoleUiHelpers.RenderInspection(resolvedDataPath, data, config, settings.PreviewRows);
        return 0;
    }
}
