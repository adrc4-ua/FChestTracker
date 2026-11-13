using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>Rounded shapes shared by every hand-painted control.</summary>
internal static class Rounded
{
    /// <summary>A rectangle with round corners; plain when the radius is too small to show.</summary>
    public static GraphicsPath Rect(RectangleF bounds, float radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        if (diameter <= 0.5f)
        {
            path.AddRectangle(bounds);
            return path;
        }
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>The usual inner box of a control: half a pixel in, so a 1 px edge lands on whole pixels.</summary>
    public static RectangleF Inner(Control control) => new(0.5f, 0.5f, control.Width - 1.5f, control.Height - 1.5f);

    /// <summary>Stores a new value and repaints the control only when it actually changed.</summary>
    public static void Assign<T>(Control control, ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            control.Invalidate();
        }
    }
}
