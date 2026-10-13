using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls.Rendering;

/// <summary>Composes positioned controls into a fixed-size terminal canvas.</summary>
public sealed class LayeredRenderable(int width, int height, IEnumerable<ControlLayer> layers) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        var visibleWidth = Math.Clamp(width, 1, Math.Max(1, maxWidth));
        return new Measurement(visibleWidth, visibleWidth);
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var canvasWidth = Math.Clamp(width, 1, Math.Max(1, maxWidth));
        var canvasHeight = Math.Max(1, height);
        var cells = new Cell[canvasHeight, canvasWidth];

        // OrderBy is stable: later layers win when their z-index values match.
        foreach (var layer in layers.OrderBy(layer => layer.ZIndex))
        {
            if (layer.X >= canvasWidth || layer.Y >= canvasHeight)
            {
                continue;
            }

            var renderWidth = Math.Max(1, layer.Width ?? (canvasWidth - Math.Max(0, layer.X)));
            var lines = Segment.SplitLines(layer.Content.Render(options, renderWidth), renderWidth, null);
            for (var row = 0; row < lines.Count; row++)
            {
                var y = layer.Y + row;
                if (y < 0 || y >= canvasHeight)
                {
                    continue;
                }

                var x = layer.X;
                foreach (var segment in lines[row])
                {
                    var elements = StringInfo.GetTextElementEnumerator(segment.Text);
                    while (elements.MoveNext())
                    {
                        var glyph = elements.GetTextElement();
                        var glyphWidth = glyph.GetCellWidth();
                        if (glyphWidth <= 0)
                        {
                            continue;
                        }

                        if (x >= 0 && x + glyphWidth <= canvasWidth)
                        {
                            for (var column = x; column < x + glyphWidth; column++)
                            {
                                ClearGlyph(cells, y, column, canvasWidth);
                            }

                            cells[y, x] = new Cell(glyph, segment.Style, segment.Link, glyphWidth);
                            for (var column = x + 1; column < x + glyphWidth; column++)
                            {
                                cells[y, column] = new Cell(string.Empty, segment.Style, segment.Link, 0);
                            }
                        }

                        x += glyphWidth;
                    }
                }
            }
        }

        for (var row = 0; row < canvasHeight; row++)
        {
            for (var column = 0; column < canvasWidth; column++)
            {
                var cell = cells[row, column];
                if (cell.Width == 0 && cell.Text is not null)
                {
                    continue;
                }

                yield return cell.Text is null
                    ? Segment.Padding(1)
                    : new Segment(cell.Text, cell.Style, cell.Link);
            }

            if (row < canvasHeight - 1)
            {
                yield return Segment.LineBreak;
            }
        }
    }

    private static void ClearGlyph(Cell[,] cells, int row, int column, int canvasWidth)
    {
        var start = column;
        while (start > 0 && cells[row, start].Text == string.Empty)
        {
            start--;
        }

        var glyphWidth = cells[row, start].Width;
        for (var index = start; index < Math.Min(canvasWidth, start + glyphWidth); index++)
        {
            cells[row, index] = default;
        }
    }

    private readonly record struct Cell(string? Text, Style Style, Link? Link, int Width);
}
