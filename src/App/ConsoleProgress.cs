namespace FCT.App;

/// <summary>
/// A progress line for the command line options, rewritten in place: caption, bar, "done / total" and, when it fits,
/// the end of the file being read. Silent when the output goes to a file or pipe (the final summary is enough there).
/// </summary>
public sealed class ConsoleProgress
{
    private const int Cells = 28;

    private readonly string _caption;
    private readonly int _steps;
    private readonly bool _live = !(Console.IsOutputRedirected || Console.IsInputRedirected);
    private int _width; // length of the last line drawn, to blank leftovers of a longer one

    public ConsoleProgress(string caption, int steps)
    {
        _caption = caption;
        _steps = steps < 1 ? 1 : steps;
    }

    public void Show(int step, string? currentFile = null)
    {
        if (!_live)
        {
            return;
        }
        var ratio = Math.Min(Math.Max(step, 0), _steps) / (double)_steps;
        var filled = (int)Math.Round(ratio * Cells);
        var parts = new (string Text, ConsoleColor Color)[]
        {
            ($"  {_caption}  ", ConsoleColor.Cyan),
            ("".PadRight(filled, '█').PadRight(Cells, '░'), ConsoleColor.Yellow),
            ($"  {step} / {_steps}" + FileSuffix(currentFile, _caption.Length + Cells + 16), ConsoleColor.Gray),
        };

        Console.Write('\r');
        var written = 0;
        foreach (var (text, color) in parts)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            written += text.Length;
        }
        Console.ResetColor();
        if (_width > written)
        {
            Console.Write("".PadRight(_width - written));
        }
        _width = written;
    }

    /// <summary>Blanks the line so the summary can take its place.</summary>
    public void Erase()
    {
        if (_live && _width > 0)
        {
            Console.Write("\r" + "".PadRight(_width) + "\r");
            _width = 0;
        }
    }

    /// <summary>"  …end-of-name" sized to the console, or nothing when there is no room.</summary>
    private static string FileSuffix(string? file, int used)
    {
        if (string.IsNullOrEmpty(file))
        {
            return "";
        }
        int columns;
        try
        {
            columns = Console.WindowWidth;
        }
        catch (IOException)
        {
            columns = 80;
        }
        var room = columns - used - 4;
        if (room <= 8)
        {
            return "";
        }
        return "  " + (file.Length <= room ? file : "…" + file.Substring(file.Length - room + 1));
    }
}
