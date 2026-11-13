using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>An on/off switch in place of Windows' white check box: gold when on, grey when off. Space or Enter flips it.</summary>
[SupportedOSPlatform("windows")]
public sealed class ToggleSwitch : Control
{
    private bool _on;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Size = new Size(40, 22);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public event EventHandler? CheckedChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _on;
        set
        {
            if (_on != value)
            {
                _on = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    protected override void OnClick(EventArgs e)
    {
        Focus();
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Checked = !Checked;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.Fill(this, e.Graphics);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var shape = Rounded.Rect(track, track.Height / 2))
        {
            if (_on)
            {
                using var gold = Theme.GoldBrush(track);
                g.FillPath(gold, shape);
            }
            else
            {
                using var fill = new SolidBrush(Theme.Bg2);
                using var edge = new Pen(Theme.Line, 1f);
                g.FillPath(fill, shape);
                g.DrawPath(edge, shape);
            }
            if (Focused)
            {
                using var ring = new Pen(Color.FromArgb(160, Theme.GoldHi), 1f);
                g.DrawPath(ring, shape);
            }
        }

        var knob = track.Height - 6;
        var x = _on ? track.Right - knob - 3 : track.Left + 3;
        using var knobFill = new SolidBrush(_on ? Theme.GoldInk : Theme.Muted);
        g.FillEllipse(knobFill, x, track.Top + 3, knob, knob);
    }
}
