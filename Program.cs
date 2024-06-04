// See https://aka.ms/new-console-template for more information
using ImageEditor;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.Text.Json;

var jsonPath = "D:\\Working Space\\Repository\\ImageEditor\\Data.json";
var listPeserta = new List<NameTagModel>();
using (StreamReader r = new StreamReader(jsonPath))
{
    string json = r.ReadToEnd();
    listPeserta = JsonSerializer.Deserialize<List<NameTagModel>>(json);
}

var basePath = "D:\\Working Space\\Repository\\ImageEditor\\BaseImage";
var filePath = "\\Dummy.png";

//var tempPeserta = new List<NameTagModel>();
//tempPeserta.Add(listPeserta.FirstOrDefault());

foreach (var peserta in listPeserta)
{
    var newfilePath = basePath + "\\Result\\" + peserta.Name + ".png";
    File.Copy(basePath + filePath, newfilePath);
    using (Image image = Image.Load(newfilePath))
    {
        Font font = SystemFonts.CreateFont("Segoe UI", 80, FontStyle.Bold);

        var namePosition = new PointF(image.Width / 2f, 1200);
        var kelompokKamarPosition = new PointF(image.Width / 2f, 1200);

        var nameOptions = new RichTextOptions(font)
        {
            Origin = namePosition,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var kelompokKamarOptions = new RichTextOptions(font)
        {
            Origin = kelompokKamarPosition,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        //var kelompokKamarText = $"Kel. {peserta.Kelompok} | Kamar {peserta.Kamar}";
        var kelompokKamarText = string.Empty;
        //image.Mutate(x => x.DrawText(options, text, font, Color.Black, position));
        image.Mutate(x => x.DrawText(nameOptions, peserta.Name, Color.Black));
        //image.Mutate(x => x.DrawText(kelompokKamarOptions, kelompokKamarText, Color.Black));

        image.Save(newfilePath);
    }
}
