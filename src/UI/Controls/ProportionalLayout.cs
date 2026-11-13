using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// Keeps a window drawn at a fixed design size: every control's place, size, corner radius and font scale with the
/// client area, so resizing or maximizing enlarges the whole window evenly. Fonts are in points: the factor is divided
/// by the screen scale so it is not counted twice.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class ProportionalLayout
{
    private sealed class Design
    {
        public Point Location;
        public Size Size;
        public Font? Font;
        public Font? Scaled;
        public int Radius; // of cards and buttons; 0 for other controls
    }

    private readonly Form _window;
    private readonly int _designWidth;
    private readonly Dictionary<Control, Design> _designs = new();
    private float _lastX = 1, _lastY = 1;

    public ProportionalLayout(Form window, int designWidth, int designHeight)
    {
        _window = window;
        _designWidth = designWidth;
        DesignHeight = designHeight;
    }

    /// <summary>The height the controls' design places refer to (windows that grow change it).</summary>
    public int DesignHeight { get; private set; }

    /// <summary>Records every control as it is now as its design (call once the window is built).</summary>
    public void Capture() => Capture(_window);

    private void Capture(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            _designs[control] = new Design
            {
                Location = control.Location,
                Size = control.Size,
                Font = control.Font,
                Radius = control switch { Card card => card.Radius, PillButton button => button.Radius, _ => 0 },
            };
            Capture(control);
        }
    }

    /// <summary>Moves a control's design place to another height.</summary>
    public void MoveTo(Control control, int designY)
    {
        if (_designs.TryGetValue(control, out var design))
        {
            design.Location = new Point(design.Location.X, designY);
        }
    }

    /// <summary>
    /// A new design height: the window keeps its current scale and grows or shrinks by the difference, then everything
    /// is placed again.
    /// </summary>
    public void Resize(int designHeight)
    {
        var scale = _window.ClientSize.Height / (float)DesignHeight;
        DesignHeight = designHeight;
        _lastX = _lastY = -1;
        if (_window.WindowState == FormWindowState.Normal)
        {
            _window.ClientSize = new Size(_window.ClientSize.Width, (int)Math.Round(DesignHeight * scale));
        }
        Apply();
    }

    /// <summary>Places, sizes and fonts for the current client size (nothing when it barely changed).</summary>
    public void Apply()
    {
        var client = _window.ClientSize;
        if (_designs.Count == 0 || client.Width <= 0 || client.Height <= 0)
        {
            return;
        }
        var x = client.Width / (float)_designWidth;
        var y = client.Height / (float)DesignHeight;
        if (Math.Abs(x - _lastX) < 0.005 && Math.Abs(y - _lastY) < 0.005)
        {
            return;
        }
        (_lastX, _lastY) = (x, y);
        // while restoring or maximizing the size can be transitional: without a sensible factor fonts are left alone
        var screen = _window.DeviceDpi / 96f;
        var fontFactor = Math.Min(x / screen, y / screen);
        var scaleFonts = float.IsFinite(fontFactor) && fontFactor > 0.2 && fontFactor < 10;

        _window.SuspendLayout();
        foreach (var (control, design) in _designs)
        {
            control.Location = new Point((int)Math.Round(design.Location.X * x), (int)Math.Round(design.Location.Y * y));
            control.Size = new Size((int)Math.Round(design.Size.Width * x), (int)Math.Round(design.Size.Height * y));
            // corners follow too, or a larger window would look squarer
            var radius = Math.Max(4, (int)Math.Round(design.Radius * Math.Min(x, y)));
            switch (control)
            {
                case Card card:
                    card.Radius = radius;
                    break;
                case PillButton button:
                    button.Radius = radius;
                    break;
            }
            if (scaleFonts && design.Font != null)
            {
                ScaleFont(control, design, fontFactor);
            }
        }
        _window.ResumeLayout();
        _window.Invalidate(true);
    }

    /// <summary>
    /// Gives the control its design font times <paramref name="factor"/>. An equal font is not set again: WinForms keeps
    /// the old one when they are equal, so disposing it would leave the control with a disposed font.
    /// </summary>
    private static void ScaleFont(Control control, Design design, float factor)
    {
        var font = new Font(design.Font!.FontFamily, design.Font.SizeInPoints * factor, design.Font.Style);
        if (design.Scaled != null && design.Scaled.Equals(font))
        {
            font.Dispose();
            return;
        }
        var previous = design.Scaled;
        control.Font = font;
        design.Scaled = font;
        previous?.Dispose();
    }
}
