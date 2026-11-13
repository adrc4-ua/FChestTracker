using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>Drop-down menus in the app's colours: dark background, a soft gold highlight and light text.</summary>
[SupportedOSPlatform("windows")]
public sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new Palette())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Theme.GoldHi : e.Item.ForeColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var highlight = Rounded.Rect(new RectangleF(2, 1, e.Item.Width - 4, e.Item.Height - 2), 6);
            using var fill = new SolidBrush(Theme.GoldSoft);
            e.Graphics.FillPath(fill, highlight);
        }
    }

    private sealed class Palette : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Card;
        public override Color MenuBorder => Theme.GoldLine;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Theme.GoldSoft;
        public override Color ImageMarginGradientBegin => Theme.Card;
        public override Color ImageMarginGradientMiddle => Theme.Card;
        public override Color ImageMarginGradientEnd => Theme.Card;
        public override Color SeparatorDark => Theme.Line;
        public override Color SeparatorLight => Theme.Card;
    }
}
