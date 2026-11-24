using ImageEditor.Core.Models;
using ImageEditor.Core.Services;
using Spectre.Console;

namespace ImageEditor.Cli.Tui;

public static class ConsoleUiHelpers
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "BaseImage", "bin", "obj", ".git", ".vs", "src", "tests", "node_modules"
    };

    public static List<string> DiscoverDataFiles(string workspaceRoot)
    {
        var files = new List<string>();
        if (!Directory.Exists(workspaceRoot))
        {
            return files;
        }

        foreach (string file in Directory.EnumerateFiles(workspaceRoot, "*.*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(file);
            if (name.Equals(ConfigService.DefaultConfigFileName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".config.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (DataLoader.SupportedExtensions.Contains(ext))
            {
                files.Add(file);
            }
        }

        // Also check a Data/ folder if the user creates one
        string dataDir = Path.Combine(workspaceRoot, "Data");
        if (Directory.Exists(dataDir))
        {
            foreach (string file in Directory.EnumerateFiles(dataDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (DataLoader.SupportedExtensions.Contains(ext))
                {
                    files.Add(file);
                }
            }
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }

    public static GenerationSummary RunWithProgress(
        NameTagRenderer renderer,
        IReadOnlyList<AttendeeRecord> records,
        TemplateConfig config,
        string workspaceRoot,
        GenerationOptions options)
    {
        GenerationSummary? summary = null;

        AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .Start(ctx =>
            {
                string label = options.DryRun ? "Validating nametags (dry run)" : "Generating nametags";
                ProgressTask task = ctx.AddTask($"[cyan]{label}[/]", maxValue: Math.Max(1, records.Count));

                var progress = new Progress<GenerationProgress>(p =>
                {
                    task.MaxValue = Math.Max(1, p.Total);
                    task.Value = p.Completed;
                    task.Description = $"[cyan]{label}[/] — [white]{Markup.Escape(p.CurrentName)}[/]";
                });

                summary = renderer.Generate(records, config, workspaceRoot, options, progress);
                task.Value = task.MaxValue;
                task.Description = $"[green]{label} complete[/]";
            });

        return summary!;
    }

    public static void RenderSummary(GenerationSummary summary)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap().PadRight(3));
        grid.AddColumn();

        grid.AddRow("[bold]Mode[/]", summary.DryRun ? "[yellow]Dry Run (no files written)[/]" : "[green]Generate PNGs[/]");
        grid.AddRow("[bold]Loaded / Processed[/]", $"{summary.TotalLoaded} loaded, {summary.TotalProcessed} processed");
        grid.AddRow("[bold]Succeeded[/]", $"[green]{summary.Succeeded}[/]");
        grid.AddRow("[bold]Failed[/]", summary.Failed > 0 ? $"[red]{summary.Failed}[/]" : "[grey]0[/]");
        grid.AddRow("[bold]Warnings[/]", summary.WarningCount > 0 ? $"[yellow]{summary.WarningCount}[/]" : "[grey]0[/]");
        grid.AddRow("[bold]Elapsed[/]", $"{summary.Duration.TotalSeconds:0.00}s");
        grid.AddRow("[bold]Output Folder[/]", $"[link]{Markup.Escape(summary.OutputDirectory)}[/]");

        var panel = new Panel(grid)
        {
            Header = new PanelHeader(" Generation Summary "),
            Border = BoxBorder.Rounded,
            BorderStyle = summary.Failed > 0 ? Style.Parse("red") : Style.Parse("green")
        };

        AnsiConsole.Write(panel);

        var issues = summary.Results
            .Where(r => !r.Success || r.Warnings.Count > 0)
            .ToList();

        if (issues.Count > 0)
        {
            const int maxRows = 15;
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title($"[yellow]Warnings & Errors ({issues.Count} records)[/]")
                .AddColumn("Row")
                .AddColumn("Name")
                .AddColumn("Status")
                .AddColumn("Details");

            foreach (var item in issues.Take(maxRows))
            {
                string status = item.Success ? "[yellow]Warning[/]" : "[red]Error[/]";
                string details = item.Success
                    ? string.Join("; ", item.Warnings)
                    : item.ErrorMessage ?? "Unknown error";

                table.AddRow(
                    item.Record.RowNumber.ToString(),
                    Markup.Escape(item.Record.Name),
                    status,
                    Markup.Escape(details));
            }

            AnsiConsole.Write(table);

            if (issues.Count > maxRows)
            {
                AnsiConsole.MarkupLine($"[grey]... and {issues.Count - maxRows} more record(s) with warnings/errors.[/]");
            }
        }
    }

    public static void RenderInspection(string filePath, DataLoadResult data, TemplateConfig config, int previewRows = 15)
    {
        int pesertaCount = data.Records.Count(r => r.HasKelompok);
        int panitiaCount = data.Records.Count(r => r.IsPanitia);
        int panitiaWithKamar = data.Records.Count(r => r.IsPanitia && r.HasKamar);
        int groupCount = data.Records
            .Where(r => r.HasKelompok)
            .Select(r => r.Kelompok)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        string longestFormatted = data.Records
            .Select(r => NameFormatter.FormatDisplayName(r.Name, config.NameMaxWords, config.ApplyTitleCase))
            .OrderByDescending(s => s.Length)
            .FirstOrDefault() ?? "-";

        var statsGrid = new Grid();
        statsGrid.AddColumn(new GridColumn().NoWrap().PadRight(3));
        statsGrid.AddColumn();

        statsGrid.AddRow("[bold]File[/]", Markup.Escape(filePath));
        statsGrid.AddRow("[bold]Total Records[/]", data.Records.Count.ToString());
        statsGrid.AddRow("[bold]Peserta (with Kelompok)[/]", $"{pesertaCount} across {groupCount} group(s)");
        statsGrid.AddRow("[bold]Panitia (no Kelompok)[/]", panitiaCount.ToString());
        if (panitiaWithKamar > 0)
        {
            statsGrid.AddRow(
                "[bold]Panitia with Kamar[/]",
                $"[yellow]{panitiaWithKamar}[/] (currently {(config.ShowKamarWhenNoKelompok ? "[green]shown[/]" : "[grey]hidden[/]")})");
        }
        statsGrid.AddRow("[bold]Longest Formatted Name[/]", $"{Markup.Escape(longestFormatted)} ({longestFormatted.Length} chars)");

        AnsiConsole.Write(new Panel(statsGrid)
        {
            Header = new PanelHeader(" Dataset Overview "),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cyan")
        });

        if (data.Warnings.Count > 0)
        {
            foreach (string warning in data.Warnings.Take(5))
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] {Markup.Escape(warning)}");
            }
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"Preview (first {Math.Min(previewRows, data.Records.Count)} of {data.Records.Count})")
            .AddColumn("#")
            .AddColumn("Raw Name")
            .AddColumn("Formatted Name")
            .AddColumn("Kelompok")
            .AddColumn("Kamar");

        foreach (var r in data.Records.Take(previewRows))
        {
            string formatted = NameFormatter.FormatDisplayName(r.Name, config.NameMaxWords, config.ApplyTitleCase);
            table.AddRow(
                r.RowNumber.ToString(),
                Markup.Escape(r.Name),
                $"[cyan]{Markup.Escape(formatted)}[/]",
                r.HasKelompok ? Markup.Escape($"{config.KelompokPrefix}{r.Kelompok}") : "[grey](Panitia)[/]",
                r.HasKamar ? Markup.Escape(r.Kamar) : "[grey]-[/]");
        }

        AnsiConsole.Write(table);
    }
}
