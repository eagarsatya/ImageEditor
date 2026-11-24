using ImageEditor.Cli.Commands;
using ImageEditor.Cli.Tui;
using Spectre.Console;
using Spectre.Console.Cli;

if (args.Length == 0)
{
    var tui = new InteractiveApp();
    return tui.Run();
}

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("ImageEditor");

    config.AddCommand<GenerateCommand>("generate")
        .WithDescription("Generate nametags from a JSON, CSV, or Excel file.")
        .WithExample("generate", "--data", "\"Peserta 1.json\"")
        .WithExample("generate", "--data", "\"Peserta 1.json\"", "--filter", "\"Agnes\"")
        .WithExample("generate", "--data", "\"Peserta 1.json\"", "--limit", "3", "--dry-run");

    config.AddCommand<InspectCommand>("inspect")
        .WithDescription("Inspect an attendee data file and preview formatted names.")
        .WithExample("inspect", "--data", "\"Peserta 1.json\"");
});

try
{
    return app.Run(args);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
    return 1;
}
