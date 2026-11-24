using ImageEditor.Core.Models;
using ImageEditor.Core.Services;
using Spectre.Console;

namespace ImageEditor.Cli.Tui;

public sealed class InteractiveApp
{
    private readonly ConfigService _configService = new();
    private readonly DataLoader _dataLoader = new();
    private readonly NameTagRenderer _renderer = new();

    public int Run()
    {
        string workspaceRoot = ConfigService.ResolveWorkspaceRoot();
        string configPath = Path.Combine(workspaceRoot, ConfigService.DefaultConfigFileName);
        TemplateConfig config = _configService.LoadOrCreateDefault(configPath);

        while (true)
        {
            AnsiConsole.Clear();
            RenderHeader(workspaceRoot, config);

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices(
                        "Generate Nametags",
                        "Inspect Attendee Data",
                        "Configure Settings & Layout",
                        "Exit"));

            try
            {
                switch (choice)
                {
                    case "Generate Nametags":
                        RunGenerateFlow(workspaceRoot, config);
                        Pause();
                        break;
                    case "Inspect Attendee Data":
                        RunInspectFlow(workspaceRoot, config);
                        Pause();
                        break;
                    case "Configure Settings & Layout":
                        RunSettingsFlow(workspaceRoot, configPath, ref config);
                        break;
                    case "Exit":
                        AnsiConsole.MarkupLine("[grey]Goodbye![/]");
                        return 0;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                Pause();
            }
        }
    }

    private static void RenderHeader(string workspaceRoot, TemplateConfig config)
    {
        AnsiConsole.Write(
            new FigletText("ImageEditor")
                .LeftJustified()
                .Color(Color.Cyan1));

        var info = new Grid();
        info.AddColumn(new GridColumn().NoWrap().PadRight(2));
        info.AddColumn();
        info.AddRow("[grey]Workspace:[/]", Markup.Escape(workspaceRoot));
        info.AddRow("[grey]Template:[/]", Markup.Escape(config.TemplateImagePath));
        info.AddRow("[grey]Output:[/]", Markup.Escape(config.OutputDirectory));
        info.AddRow("[grey]Font:[/]", $"{Markup.Escape(config.FontFamily)} ({config.FontSize:0.#}pt, auto-fit min {config.MinFontSize:0.#}pt)");

        AnsiConsole.Write(new Panel(info)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("grey")
        });
        AnsiConsole.WriteLine();
    }

    private void RunGenerateFlow(string workspaceRoot, TemplateConfig config)
    {
        string? dataPath = PromptForDataFile(workspaceRoot);
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            return;
        }

        DataLoadResult data = _dataLoader.Load(dataPath);
        if (data.Records.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No valid attendee records found in that file.[/]");
            return;
        }

        AnsiConsole.MarkupLine(
            $"[green]Loaded {data.Records.Count} record(s)[/] " +
            $"({data.Records.Count(r => r.HasKelompok)} Peserta, {data.Records.Count(r => r.IsPanitia)} Panitia).");

        if (data.Warnings.Count > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{data.Warnings.Count} load warning(s) (use 'Inspect' to view details).[/]");
        }

        string mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select generation scope:")
                .AddChoices(
                    "All attendees",
                    "Preview first N attendees",
                    "Search by name",
                    "Filter by Kelompok",
                    "Dry run (validate all without writing files)",
                    "Back"));

        if (mode == "Back")
        {
            return;
        }

        var options = new GenerationOptions();

        switch (mode)
        {
            case "Preview first N attendees":
                options.Limit = AnsiConsole.Prompt(
                    new TextPrompt<int>("How many nametags to generate?")
                        .DefaultValue(1)
                        .Validate(n => n > 0
                            ? ValidationResult.Success()
                            : ValidationResult.Error("Must be greater than 0.")));
                break;

            case "Search by name":
                options.NameFilter = AnsiConsole.Ask<string>("Enter name (or partial name) to match:");
                break;

            case "Filter by Kelompok":
                var groups = data.Records
                    .Where(r => r.HasKelompok)
                    .Select(r => r.Kelompok)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (data.Records.Any(r => r.IsPanitia))
                {
                    groups.Add("Panitia");
                }

                if (groups.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No groups found in this dataset.[/]");
                    return;
                }

                options.KelompokFilter = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Choose Kelompok:")
                        .PageSize(15)
                        .AddChoices(groups));
                break;

            case "Dry run (validate all without writing files)":
                options.DryRun = true;
                break;
        }

        IReadOnlyList<AttendeeRecord> matched = _renderer.ApplyFilters(data.Records, options);
        if (matched.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No records matched your filter.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"Ready to process [bold cyan]{matched.Count}[/] nametag(s).");
        if (!AnsiConsole.Confirm("Proceed?", defaultValue: true))
        {
            AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
            return;
        }

        GenerationSummary summary = ConsoleUiHelpers.RunWithProgress(
            _renderer,
            data.Records,
            config,
            workspaceRoot,
            options);

        AnsiConsole.WriteLine();
        ConsoleUiHelpers.RenderSummary(summary);
    }

    private void RunInspectFlow(string workspaceRoot, TemplateConfig config)
    {
        string? dataPath = PromptForDataFile(workspaceRoot);
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            return;
        }

        DataLoadResult data = _dataLoader.Load(dataPath);
        ConsoleUiHelpers.RenderInspection(dataPath, data, config, previewRows: 20);
    }

    private void RunSettingsFlow(string workspaceRoot, string configPath, ref TemplateConfig config)
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderHeader(workspaceRoot, config);

            string setting = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Select a setting to edit:[/]")
                    .PageSize(14)
                    .AddChoices(
                        $"Template Image Path: {config.TemplateImagePath}",
                        $"Output Directory: {config.OutputDirectory}",
                        $"Font Family: {config.FontFamily}",
                        $"Font Size: {config.FontSize:0.#}pt (Min: {config.MinFontSize:0.#}pt)",
                        $"Text Color: {config.TextColor}",
                        $"Name Max Words: {(config.NameMaxWords == 0 ? "Unlimited" : config.NameMaxWords)}",
                        $"Apply Title Case: {config.ApplyTitleCase}",
                        $"Kelompok Prefix: \"{config.KelompokPrefix}\"",
                        $"Show Kamar When No Kelompok (Panitia): {config.ShowKamarWhenNoKelompok}",
                        $"Organize Output By Kelompok: {config.OrganizeByKelompok}",
                        $"Layout Y Positions (Name={config.Layout.NameY}, NameOnly={config.Layout.NameOnlyY}, Kel={config.Layout.KelompokY}, Kamar={config.Layout.KamarY})",
                        "Reset to Defaults",
                        "Back"));

            if (setting == "Back")
            {
                return;
            }

            if (setting.StartsWith("Template Image Path:"))
            {
                config.TemplateImagePath = AnsiConsole.Ask("Template image path (relative or absolute):", config.TemplateImagePath);
            }
            else if (setting.StartsWith("Output Directory:"))
            {
                config.OutputDirectory = AnsiConsole.Ask("Output directory (relative or absolute):", config.OutputDirectory);
            }
            else if (setting.StartsWith("Font Family:"))
            {
                config.FontFamily = AnsiConsole.Ask("Font family name:", config.FontFamily);
            }
            else if (setting.StartsWith("Font Size:"))
            {
                config.FontSize = AnsiConsole.Ask("Preferred font size (pt):", config.FontSize);
                config.MinFontSize = AnsiConsole.Ask("Minimum auto-fit font size (pt):", config.MinFontSize);
            }
            else if (setting.StartsWith("Text Color:"))
            {
                config.TextColor = AnsiConsole.Ask("Text color hex (e.g. #000000):", config.TextColor);
            }
            else if (setting.StartsWith("Name Max Words:"))
            {
                config.NameMaxWords = AnsiConsole.Ask("Max words for name (0 = unlimited):", config.NameMaxWords);
            }
            else if (setting.StartsWith("Apply Title Case:"))
            {
                config.ApplyTitleCase = AnsiConsole.Confirm("Apply Title Case to names?", config.ApplyTitleCase);
            }
            else if (setting.StartsWith("Kelompok Prefix:"))
            {
                config.KelompokPrefix = AnsiConsole.Ask("Prefix before Kelompok value:", config.KelompokPrefix);
            }
            else if (setting.StartsWith("Show Kamar When No Kelompok"))
            {
                config.ShowKamarWhenNoKelompok = AnsiConsole.Confirm("Show Kamar even when Kelompok is empty?", config.ShowKamarWhenNoKelompok);
            }
            else if (setting.StartsWith("Organize Output By Kelompok:"))
            {
                config.OrganizeByKelompok = AnsiConsole.Confirm("Group generated PNGs into subfolders by Kelompok?", config.OrganizeByKelompok);
            }
            else if (setting.StartsWith("Layout Y Positions"))
            {
                config.Layout.NameY = AnsiConsole.Ask("Name Y position (px):", config.Layout.NameY);
                config.Layout.NameOnlyY = AnsiConsole.Ask("Name-only Y position for Panitia (px):", config.Layout.NameOnlyY);
                config.Layout.KelompokY = AnsiConsole.Ask("Kelompok Y position (px):", config.Layout.KelompokY);
                config.Layout.KamarY = AnsiConsole.Ask("Kamar Y position (px):", config.Layout.KamarY);
            }
            else if (setting == "Reset to Defaults")
            {
                if (AnsiConsole.Confirm("Reset all settings in nametag.config.json to defaults?", false))
                {
                    config = new TemplateConfig();
                }
            }

            _configService.Save(config, configPath);
            AnsiConsole.MarkupLine("[green]Saved to nametag.config.json[/]");
        }
    }

    private static string? PromptForDataFile(string workspaceRoot)
    {
        List<string> discovered = ConsoleUiHelpers.DiscoverDataFiles(workspaceRoot);
        var choices = new List<string>();
        var labelToPath = new Dictionary<string, string>();

        foreach (string path in discovered)
        {
            string relative = Path.GetRelativePath(workspaceRoot, path);
            choices.Add(relative);
            labelToPath[relative] = path;
        }

        const string customOption = "Enter custom file path...";
        const string backOption = "Back";
        choices.Add(customOption);
        choices.Add(backOption);

        string selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select attendee data file ([grey].json, .csv, .xlsx[/]):")
                .PageSize(12)
                .AddChoices(choices));

        if (selected == backOption)
        {
            return null;
        }

        if (selected == customOption)
        {
            string input = AnsiConsole.Ask<string>("Enter path to data file:");
            return ConfigService.ResolvePath(input.Trim('"'), workspaceRoot);
        }

        return labelToPath[selected];
    }

    private static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[grey]Press any key to return to the menu...[/]");
        Console.ReadKey(intercept: true);
    }
}
