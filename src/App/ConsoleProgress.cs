namespace FCT.App;

/// <summary>
/// A progress line for the command line options, rewritten in place: label, bar, "done / total" and, when it fits,
/// the end of the current file name. Draws nothing when the output is redirected (the final summary is enough).
/// </summary>
public sealed class ConsoleProgressBar
{
    private const int BarCells = 28;
    private static readonly bool CanRedraw = !Console.IsInputRedirected && !Console.IsOutputRedirected;

    private readonly string _label;
    private readonly int _total;
    private int _drawn; // characters of the previous line, to blank what a shorter one leaves behind

    public ConsoleProgressBar(string label, int total)
    {
        _label = label;
        _total = Math.Max(1, total);
    }

    public void Update(int done, string? detail = null)
    {
        if (!CanRedraw)
        {
            return;
        }
        var full = (int)Math.Round(BarCells * Math.Clamp(done, 0, _total) / (double)_total);
        var bar = new string('█', full) + new string('░', BarCells - full);
        var tail = $"  {done} / {_total}";
        var head = $"  {_label}  ";

        var used = head.Length + bar.Length + tail.Length;
        if (!string.IsNullOrEmpty(detail))
        {
            var space = ConsoleWidth() - used - 4;
            if (space > 8)
            {
                tail += "  " + (detail.Length > space ? "…" + detail[^(space - 1)..] : detail);
            }
        }

        Console.Write('\r');
        Paint(head, ConsoleColor.Cyan);
        Paint(bar, ConsoleColor.Yellow);
        Paint(tail, ConsoleColor.Gray);
        Console.ResetColor();
        var length = head.Length + bar.Length + tail.Length;
        if (length < _drawn)
        {
            Console.Write(new string(' ', _drawn - length));
        }
        _drawn = length;
    }

    /// <summary>Blanks the line, so the summary can take its place.</summary>
    public void Clear()
    {
        if (CanRedraw && _drawn > 0)
        {
            Console.Write('\r' + new string(' ', _drawn) + '\r');
            _drawn = 0;
        }
    }

    private static void Paint(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
    }

    private static int ConsoleWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            return 80;
        }
    }
}
