using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// The language button: "Sistema (…)" (Windows' language, the default) or one of the languages, each named in itself.
/// A click opens a dark menu with a check on the current one.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class LanguagePicker
{
    private const string Check = "✓   ", NoCheck = "      ";
    private readonly ContextMenuStrip _menu;
    private string? _chosen;

    /// <param name="chosen">The saved language code; one no longer offered counts as Windows'.</param>
    /// <param name="changed">Called with the new code (null for Windows') when another one is picked.</param>
    public LanguagePicker(string? chosen, Point at, Action<string?> changed)
    {
        _chosen = chosen != null && L.Languages.Any(l => l.Code == chosen) ? chosen : null;
        Button = new PillButton
        {
            Text = NameOf(_chosen), Dropdown = true, Location = at, Size = new Size(172, 30), Radius = 10,
            Font = Theme.Text(8.5f, FontStyle.Bold), BackColor = Theme.Card,
        };
        _menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(), ShowImageMargin = false, ShowCheckMargin = false,
            BackColor = Theme.Card, Font = Theme.Text(9f), Padding = new Padding(4),
        };
        AddChoice(null, changed);
        _menu.Items.Add(new ToolStripSeparator());
        foreach (var (code, _) in L.Languages)
        {
            AddChoice(code, changed);
        }
        Button.ContextMenuStrip = _menu; // the right click opens it too
        Button.Click += (_, _) => _menu.Show(Button, new Point(0, Button.Height + 4));
    }

    public PillButton Button { get; }

    private static string NameOf(string? code) => code == null
        ? L.F("Sistema ({0})", "System ({0})", L.Languages.FirstOrDefault(l => l.Code == L.SystemCode).Name)
        : L.Languages.First(l => l.Code == code).Name;

    private void AddChoice(string? code, Action<string?> changed)
    {
        var mine = code == _chosen;
        var item = new ToolStripMenuItem((mine ? Check : NoCheck) + NameOf(code))
        {
            ForeColor = mine ? Theme.GoldHi : Theme.Ink,
            Padding = new Padding(4, 3, 18, 3),
        };
        item.Click += (_, _) =>
        {
            if (code != _chosen)
            {
                Choose(code);
                changed(code);
            }
        };
        _menu.Items.Add(item);
    }

    private void Choose(string? code)
    {
        _chosen = code;
        Button.Text = NameOf(code);
        var name = NameOf(code);
        foreach (var item in _menu.Items.OfType<ToolStripMenuItem>())
        {
            var text = item.Text?.TrimStart('✓', ' ');
            var mine = text == name;
            item.Text = (mine ? Check : NoCheck) + text;
            item.ForeColor = mine ? Theme.GoldHi : Theme.Ink;
        }
    }
}
