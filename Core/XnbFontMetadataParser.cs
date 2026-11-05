using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace UntranslatedStardew.Core;

/// <summary>
/// Parsed metadata extracted from an XNB SpriteFont asset without initializing GPU textures.
/// </summary>
public sealed class SpriteFontMetadata
{
    public int LineSpacing { get; set; }
    public float Spacing { get; set; }
    public char? DefaultCharacter { get; set; }
    public List<char> Characters { get; } = new();
    public List<Rectangle> Glyphs { get; } = new();
    public List<Rectangle> Cropping { get; } = new();
    public List<Vector3> Kerning { get; } = new();

    /// <summary>
    /// Creates a headless SpriteFont instance with null Texture2D.
    /// Fully capable of headless string measurement and character inspection.
    /// </summary>
    public SpriteFont CreateHeadlessSpriteFont()
    {
        ConstructorInfo? ctor = typeof(SpriteFont).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
        if (ctor == null)
            throw new InvalidOperationException("Could not find SpriteFont constructor.");

        // If default character is specified, it must be contained in the characters list
        char? defaultChar = DefaultCharacter;
        if (defaultChar.HasValue && !Characters.Contains(defaultChar.Value))
        {
            defaultChar = null;
        }

        object?[] parameters = new object?[]
        {
            null, // Texture2D
            Glyphs,
            Cropping,
            Characters,
            LineSpacing,
            Spacing,
            Kerning,
            defaultChar
        };

        return (SpriteFont)ctor.Invoke(parameters);
    }
}

/// <summary>
/// Headless XNB parser for SpriteFont assets.
/// Decodes type reader headers, decompresses LZX compressed payloads, skips Texture2D data,
/// and extracts glyph metadata without requiring a GraphicsDevice.
/// </summary>
public static class XnbFontMetadataParser
{
    private static readonly MethodInfo? GetContentReaderMethod = typeof(ContentManager).GetMethod(
        "GetContentReaderFromXnb",
        BindingFlags.NonPublic | BindingFlags.Instance
    );

    private static readonly MethodInfo? Read7BitEncodedIntMethod = typeof(ContentReader).GetMethod(
        "Read7BitEncodedInt",
        BindingFlags.NonPublic | BindingFlags.Instance
    );

    /// <summary>
    /// Parses SpriteFont metadata directly from an XNB file on disk.
    /// Handles uncompressed, LZX compressed (flags 0x01 and 0x81), and HiDef XNBs.
    /// </summary>
    public static SpriteFontMetadata ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"XNB font file not found: {filePath}", filePath);

        using FileStream fs = File.OpenRead(filePath);
        using BinaryReader br = new(fs);

        string rootDir = Path.GetDirectoryName(filePath) ?? ".";
        string assetName = Path.GetFileNameWithoutExtension(filePath);

        // Dummy service container to satisfy ContentManager constructor
        GameServiceContainer services = new();
        ContentManager cm = new(services, rootDir);

        if (GetContentReaderMethod == null)
            throw new InvalidOperationException("MonoGame ContentManager.GetContentReaderFromXnb method not found.");

        object? readerObj = GetContentReaderMethod.Invoke(cm, new object?[] { assetName, fs, br, null });
        if (readerObj is not ContentReader reader)
            throw new InvalidOperationException("Failed to obtain ContentReader from XNB.");

        try
        {
            return ParseFromContentReader(reader);
        }
        finally
        {
            reader.Dispose();
            cm.Dispose();
        }
    }

    private static SpriteFontMetadata ParseFromContentReader(ContentReader reader)
    {
        if (Read7BitEncodedIntMethod == null)
            throw new InvalidOperationException("ContentReader.Read7BitEncodedInt method not found.");

        int numTypeReaders = (int)Read7BitEncodedIntMethod.Invoke(reader, null)!;
        for (int i = 0; i < numTypeReaders; i++)
        {
            reader.ReadString();
            reader.ReadInt32();
        }

        // Shared resources and primary type index
        Read7BitEncodedIntMethod.Invoke(reader, null);
        Read7BitEncodedIntMethod.Invoke(reader, null);

        // Texture2D payload: read header and skip pixel data
        Read7BitEncodedIntMethod.Invoke(reader, null); // Texture2D reader type index
        reader.ReadInt32(); // SurfaceFormat
        reader.ReadInt32(); // Width
        reader.ReadInt32(); // Height
        int mipCount = reader.ReadInt32();
        for (int i = 0; i < mipCount; i++)
        {
            int mipSize = reader.ReadInt32();
            reader.ReadBytes(mipSize);
        }

        SpriteFontMetadata meta = new();

        // Glyphs list
        Read7BitEncodedIntMethod.Invoke(reader, null); // Glyphs list type index
        int glyphCount = reader.ReadInt32();
        for (int i = 0; i < glyphCount; i++)
        {
            meta.Glyphs.Add(new Rectangle(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32()
            ));
        }

        // Cropping list
        Read7BitEncodedIntMethod.Invoke(reader, null); // Cropping list type index
        int cropCount = reader.ReadInt32();
        for (int i = 0; i < cropCount; i++)
        {
            meta.Cropping.Add(new Rectangle(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32()
            ));
        }

        // Characters list
        Read7BitEncodedIntMethod.Invoke(reader, null); // Char list type index
        int charCount = reader.ReadInt32();
        for (int i = 0; i < charCount; i++)
        {
            meta.Characters.Add(reader.ReadChar());
        }

        // LineSpacing and spacing
        meta.LineSpacing = reader.ReadInt32();
        meta.Spacing = reader.ReadSingle();

        // Kerning list
        Read7BitEncodedIntMethod.Invoke(reader, null); // Kerning list type index
        int kerningCount = reader.ReadInt32();
        for (int i = 0; i < kerningCount; i++)
        {
            meta.Kerning.Add(new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            ));
        }

        // Default character
        bool hasDefault = reader.ReadBoolean();
        if (hasDefault)
        {
            meta.DefaultCharacter = reader.ReadChar();
        }

        return meta;
    }
}
