using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UntranslatedStardew.Core;

/// <summary>
/// A single measured text segment assigned to a specific SpriteFont.
/// </summary>
public sealed class TextRun
{
    public string Text { get; }
    public SpriteFont Font { get; }
    public Vector2 Offset { get; }
    public Vector2 Size { get; }

    public TextRun(string text, SpriteFont font, Vector2 offset, Vector2 size)
    {
        Text = text;
        Font = font;
        Offset = offset;
        Size = size;
    }
}

/// <summary>
/// Pre-calculated run segmentation and measurement plan for a bilingual text block.
/// </summary>
public sealed class TextRunPlan
{
    public IReadOnlyList<TextRun> Runs { get; }
    public Vector2 TotalSize { get; }
    public int LineCount { get; }

    public TextRunPlan(IReadOnlyList<TextRun> runs, Vector2 totalSize, int lineCount)
    {
        Runs = runs;
        TotalSize = totalSize;
        LineCount = lineCount;
    }
}
