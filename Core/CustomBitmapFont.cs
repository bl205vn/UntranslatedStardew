using System.Xml.Linq;
using BmFont;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace UntranslatedStardew.Core;

internal static class CustomBitmapFont
{
    public static (SpriteFont Font, Texture2D Atlas) Load(string asset, float zoom)
    {
        var xml = XDocument.Parse(Game1.content.Load<XmlSource>(asset).Source);
        XElement root = xml.Root ?? throw new FormatException("Missing BMFont root.");
        XElement common = root.Element("common") ?? throw new FormatException("Missing BMFont metrics.");
        int lineHeight = Number(common, "lineHeight");
        if (lineHeight <= 0 || !float.IsFinite(zoom) || zoom <= 0 || zoom > 8)
            throw new FormatException("Invalid BMFont line height or scale.");

        int Scale(int value) => checked((int)MathF.Round(value * zoom));
        int slash = asset.LastIndexOf('/');
        string directory = slash < 0 ? "" : asset[..(slash + 1)];
        var pages = (root.Element("pages")?.Elements("page") ?? Enumerable.Empty<XElement>())
            .ToDictionary(page => Number(page, "id"), page =>
            {
                string file = (string?)page.Attribute("file") ?? throw new FormatException("Missing BMFont page name.");
                if (Path.IsPathRooted(file) || file.Contains(':') || file.Split('/', '\\').Contains(".."))
                    throw new FormatException("Invalid BMFont page name.");
                string pageAsset = file.Replace('\\', '/');
                if (pageAsset.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    pageAsset = pageAsset[..^4];
                try { return Game1.content.Load<Texture2D>("Fonts/" + pageAsset); }
                catch (ContentLoadException) { return Game1.content.Load<Texture2D>(directory + pageAsset); }
            });
        if (pages.Count == 0)
            throw new FormatException("No BMFont pages.");

        // Place individual glyphs into a single atlas accepted by MonoGame SpriteFont.
        const int atlasLimit = 4096;
        var bounds = new List<Rectangle>();
        var cropping = new List<Rectangle>();
        var characters = new List<char>();
        var kerning = new List<Vector3>();
        var copies = new List<(int Page, Rectangle Source, Rectangle Target)>();
        int nextX = 0, nextY = 0, rowHeight = 0, usedWidth = 1;
        foreach (var glyph in (root.Element("chars")?.Elements("char") ?? Enumerable.Empty<XElement>()).OrderBy(g => Number(g, "id")))
        {
            int code = Number(glyph, "id");
            if (code < 0 || code > char.MaxValue || char.IsSurrogate((char)code))
                continue;
            int page = Number(glyph, "page");
            var source = new Rectangle(Number(glyph, "x"), Number(glyph, "y"), Number(glyph, "width"), Number(glyph, "height"));
            if (source.X < 0 || source.Y < 0 || source.Width < 0 || source.Height < 0
                || source.Right > pages[page].Width || source.Bottom > pages[page].Height)
                throw new FormatException("BMFont glyph is outside its page.");

            int width = source.Width == 0 ? 0 : Math.Max(1, Scale(source.Width));
            int height = source.Height == 0 ? 0 : Math.Max(1, Scale(source.Height));
            if (nextX + width > atlasLimit)
            {
                nextX = 0;
                nextY += rowHeight;
                rowHeight = 0;
            }
            if (width > atlasLimit || nextY + height > atlasLimit)
                throw new FormatException("BMFont atlas exceeds the supported texture size.");

            var target = new Rectangle(nextX, nextY, width, height);
            int left = Scale(Number(glyph, "xoffset"));
            bounds.Add(target);
            cropping.Add(new Rectangle(0, Scale(Number(glyph, "yoffset")), width, height));
            characters.Add((char)code);
            kerning.Add(new Vector3(left, width, Scale(Number(glyph, "xadvance")) - left - width));
            if (width > 0 && height > 0)
                copies.Add((page, source, target));
            usedWidth = Math.Max(usedWidth, nextX + width);
            nextX += width + 1;
            rowHeight = Math.Max(rowHeight, height + 1);
        }
        if (characters.Count == 0)
            throw new FormatException("BMFont has no supported glyphs.");

        int usedHeight = Math.Min(atlasLimit, Math.Max(1, nextY + rowHeight));
        var pixels = new Color[usedWidth * usedHeight];
        foreach (var group in copies.GroupBy(copy => copy.Page))
        {
            Texture2D page = pages[group.Key];
            var sourcePixels = new Color[checked(page.Width * page.Height)];
            page.GetData(sourcePixels);
            foreach (var (_, source, target) in group)
                for (int y = 0; y < target.Height; y++)
                    for (int x = 0; x < target.Width; x++)
                        pixels[(target.Y + y) * usedWidth + target.X + x] =
                            sourcePixels[(source.Y + y * source.Height / target.Height) * page.Width + source.X + x * source.Width / target.Width];
        }

        var atlas = new Texture2D(pages.Values.First().GraphicsDevice, usedWidth, usedHeight);
        try
        {
            atlas.SetData(pixels);
            char? fallback = characters.Contains('?') ? '?' : characters.Contains(' ') ? ' ' : characters[0];
            return (new SpriteFont(atlas, bounds, cropping, characters, Math.Max(1, Scale(lineHeight)), 0f, kerning, fallback), atlas);
        }
        catch { atlas.Dispose(); throw; }
    }

    private static int Number(XElement element, string name)
        => (int?)element.Attribute(name) ?? throw new FormatException($"Missing BMFont field {name}.");
}
