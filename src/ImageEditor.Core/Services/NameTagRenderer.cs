using System.Diagnostics;
using ImageEditor.Core.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace ImageEditor.Core.Services;

public sealed class NameTagRenderer
{
    public IReadOnlyList<AttendeeRecord> ApplyFilters(
        IReadOnlyList<AttendeeRecord> records,
        GenerationOptions options)
    {
        IEnumerable<AttendeeRecord> query = records;

        if (!string.IsNullOrWhiteSpace(options.NameFilter))
        {
            string filter = options.NameFilter.Trim();
            query = query.Where(r =>
                r.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(options.KelompokFilter))
        {
            string kelFilter = options.KelompokFilter.Trim();
            if (kelFilter.Equals("panitia", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.IsPanitia);
            }
            else
            {
                query = query.Where(r =>
                    r.Kelompok.Equals(kelFilter, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (options.Limit is > 0)
        {
            query = query.Take(options.Limit.Value);
        }

        return query.ToList();
    }

    public GenerationSummary Generate(
        IReadOnlyList<AttendeeRecord> allRecords,
        TemplateConfig config,
        string workspaceRoot,
        GenerationOptions? options = null,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new GenerationOptions();
        var stopwatch = Stopwatch.StartNew();

        IReadOnlyList<AttendeeRecord> targets = ApplyFilters(allRecords, options);

        string templatePath = ConfigService.ResolvePath(
            options.TemplateImagePathOverride ?? config.TemplateImagePath,
            workspaceRoot);

        string outputRoot = ConfigService.ResolvePath(
            options.OutputDirectoryOverride ?? config.OutputDirectory,
            workspaceRoot);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template image not found: {templatePath}", templatePath);
        }

        FontFamily fontFamily = ResolveFontFamily(config, workspaceRoot);
        FontStyle fontStyle = config.FontBold ? FontStyle.Bold : FontStyle.Regular;
        Color textColor = ParseColor(config.TextColor);

        if (!options.DryRun)
        {
            Directory.CreateDirectory(outputRoot);
        }

        var results = new List<RecordGenerationResult>(targets.Count);
        var usedFileNamesByDir = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        using Image templateImage = Image.Load(templatePath);
        float maxTextWidth = templateImage.Width * Math.Clamp(config.MaxTextWidthRatio, 0.2f, 1.0f);
        float centerX = templateImage.Width * Math.Clamp(config.Layout.XRatio, 0.0f, 1.0f);

        for (int i = 0; i < targets.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AttendeeRecord record = targets[i];
            string formattedName = NameFormatter.FormatDisplayName(
                record.Name,
                config.NameMaxWords,
                config.ApplyTitleCase);

            var warnings = new List<string>();

            try
            {
                string targetDir = ResolveTargetDirectory(outputRoot, record, config);
                if (!usedFileNamesByDir.TryGetValue(targetDir, out var usedNames))
                {
                    usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    usedFileNamesByDir[targetDir] = usedNames;
                }

                string fileName = NameFormatter.BuildUniqueFileName(record, formattedName, usedNames);
                string outputPath = Path.Combine(targetDir, fileName);

                bool showKelompok = record.HasKelompok;
                bool showKamar = record.HasKamar && (record.HasKelompok || config.ShowKamarWhenNoKelompok);

                if (record.IsPanitia && record.HasKamar && !config.ShowKamarWhenNoKelompok)
                {
                    warnings.Add($"Kamar '{record.Kamar}' hidden because Kelompok is empty (Panitia mode).");
                }

                float nameY = (!showKelompok && !showKamar)
                    ? config.Layout.NameOnlyY
                    : config.Layout.NameY;

                Font nameFont = FitFont(fontFamily, fontStyle, formattedName, config.FontSize, config.MinFontSize, maxTextWidth, out bool nameShrunk);
                if (nameShrunk)
                {
                    warnings.Add($"Name auto-shrunk to {nameFont.Size:0.#}pt to fit width.");
                }

                string kelompokText = showKelompok ? $"{config.KelompokPrefix}{record.Kelompok}" : string.Empty;
                string kamarText = showKamar ? record.Kamar : string.Empty;

                Font? kelompokFont = showKelompok
                    ? FitFont(fontFamily, fontStyle, kelompokText, config.FontSize, config.MinFontSize, maxTextWidth, out _)
                    : null;

                bool kamarShrunk = false;
                Font? kamarFont = showKamar
                    ? FitFont(fontFamily, fontStyle, kamarText, config.FontSize, config.MinFontSize, maxTextWidth, out kamarShrunk)
                    : null;

                if (kamarShrunk && kamarFont is not null)
                {
                    warnings.Add($"Kamar auto-shrunk to {kamarFont.Size:0.#}pt to fit width.");
                }

                if (!options.DryRun)
                {
                    Directory.CreateDirectory(targetDir);

                    if (!config.OverwriteExisting && File.Exists(outputPath))
                    {
                        warnings.Add("Skipped writing because file already exists and OverwriteExisting is false.");
                    }
                    else
                    {
                        using Image image = templateImage.Clone(_ => { });

                        image.Mutate(ctx =>
                        {
                            ctx.DrawText(CreateTextOptions(nameFont, centerX, nameY), formattedName, textColor);

                            if (showKelompok && kelompokFont is not null)
                            {
                                ctx.DrawText(
                                    CreateTextOptions(kelompokFont, centerX, config.Layout.KelompokY),
                                    kelompokText,
                                    textColor);
                            }

                            if (showKamar && kamarFont is not null)
                            {
                                float kamarY = showKelompok ? config.Layout.KamarY : config.Layout.KelompokY;
                                ctx.DrawText(
                                    CreateTextOptions(kamarFont, centerX, kamarY),
                                    kamarText,
                                    textColor);
                            }
                        });

                        image.SaveAsPng(outputPath);
                    }
                }

                results.Add(new RecordGenerationResult
                {
                    Record = record,
                    Success = true,
                    FormattedName = formattedName,
                    OutputPath = outputPath,
                    Warnings = warnings
                });
            }
            catch (Exception ex)
            {
                results.Add(new RecordGenerationResult
                {
                    Record = record,
                    Success = false,
                    FormattedName = formattedName,
                    Warnings = warnings,
                    ErrorMessage = ex.Message
                });
            }

            progress?.Report(new GenerationProgress
            {
                Completed = i + 1,
                Total = targets.Count,
                CurrentName = formattedName
            });
        }

        stopwatch.Stop();

        return new GenerationSummary
        {
            TotalLoaded = allRecords.Count,
            TotalProcessed = targets.Count,
            Succeeded = results.Count(r => r.Success),
            Failed = results.Count(r => !r.Success),
            WarningCount = results.Sum(r => r.Warnings.Count),
            Duration = stopwatch.Elapsed,
            OutputDirectory = outputRoot,
            DryRun = options.DryRun,
            Results = results
        };
    }

    private static string ResolveTargetDirectory(string outputRoot, AttendeeRecord record, TemplateConfig config)
    {
        if (!config.OrganizeByKelompok)
        {
            return outputRoot;
        }

        string folderName = record.HasKelompok
            ? NameFormatter.SanitizeFileName($"Kel-{record.Kelompok}")
            : "Panitia";

        return Path.Combine(outputRoot, folderName);
    }

    private static RichTextOptions CreateTextOptions(Font font, float x, float y) =>
        new(font)
        {
            Origin = new PointF(x, y),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

    public static Font FitFont(
        FontFamily family,
        FontStyle style,
        string text,
        float preferredSize,
        float minSize,
        float maxWidth,
        out bool wasShrunk)
    {
        wasShrunk = false;
        float size = Math.Max(preferredSize, minSize);
        Font font = family.CreateFont(size, style);

        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0)
        {
            return font;
        }

        FontRectangle measured = TextMeasurer.MeasureSize(text, new TextOptions(font));
        while (measured.Width > maxWidth && size > minSize)
        {
            size = Math.Max(minSize, size - 2f);
            font = family.CreateFont(size, style);
            measured = TextMeasurer.MeasureSize(text, new TextOptions(font));
            wasShrunk = true;
        }

        return font;
    }

    private static FontFamily ResolveFontFamily(TemplateConfig config, string workspaceRoot)
    {
        if (!string.IsNullOrWhiteSpace(config.FontFilePath))
        {
            string resolvedFontPath = ConfigService.ResolvePath(config.FontFilePath, workspaceRoot);
            if (!File.Exists(resolvedFontPath))
            {
                throw new FileNotFoundException($"Custom font file not found: {resolvedFontPath}", resolvedFontPath);
            }

            var collection = new FontCollection();
            return collection.Add(resolvedFontPath);
        }

        if (SystemFonts.TryGet(config.FontFamily, out FontFamily family))
        {
            return family;
        }

        foreach (string fallback in new[] { "Segoe UI", "Arial", "Calibri", "DejaVu Sans", "Liberation Sans" })
        {
            if (SystemFonts.TryGet(fallback, out FontFamily fallbackFamily))
            {
                return fallbackFamily;
            }
        }

        return SystemFonts.Families.First();
    }

    private static Color ParseColor(string hex)
    {
        if (Color.TryParseHex(hex, out Color color))
        {
            return color;
        }

        return Color.Black;
    }
}
