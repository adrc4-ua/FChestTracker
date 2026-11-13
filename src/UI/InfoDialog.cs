using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>Un punto del diálogo: símbolo de color, título corto y explicación.</summary>
public sealed record InfoPoint(string Glyph, Color Color, string Title, string Text);

/// <summary>
/// Diálogo de confirmación con el estilo de la página (negro y oro: el cofre del logo, tarjeta con velo dorado,
/// botón principal de oro), en lugar del cuadro gris de Windows. Devuelve true si se pulsa el botón principal.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class InfoDialog : ThemedForm
{
    private const int W = 440, Pad = 20, TextX = 72;

    private InfoDialog(string title, string intro, IReadOnlyList<InfoPoint> points, string ok, string? cancel, bool owned)
    {
        Text = title;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = !owned; // sin ventana dueña (primer uso), que se vea en la barra de tareas
        StartPosition = owned ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
        Icon = AppIcon.Load();

        // Cabecera: el cofre del logo (con su brillo dorado), título en oro claro e introducción.
        Controls.Add(new LogoBox { Location = new Point(Pad - 4, Pad - 6), Size = new Size(48, 48), GlowAlpha = 70 });

        var y = Pad - 1;
        y = AddText(this, title, Theme.Text(12f, FontStyle.Bold), Theme.GoldHi, TextX, y, W - TextX - Pad) + 4;
        y = AddText(this, intro, Theme.Text(9f), Theme.Muted, TextX, y, W - TextX - Pad);
        y = Math.Max(y, Pad + 40) + 16;

        // Puntos, dentro de una tarjeta (sin puntos, no hay tarjeta).
        var card = new Card { Location = new Point(Pad, y), Radius = 16, BackColor = Theme.Card, Visible = points.Count > 0 };
        var cardW = W - 2 * Pad;
        var cy = 14;
        foreach (var p in points)
        {
            var badge = new Card { Location = new Point(14, cy), Size = new Size(26, 26), Radius = 8, BackColor = Theme.GoldSoft, Edge = Theme.GoldLine };
            badge.Controls.Add(new Label
            {
                Text = p.Glyph,
                Font = Theme.Text(10f, FontStyle.Bold),
                ForeColor = p.Color,
                BackColor = Color.Transparent,
                Location = new Point(0, 0),
                Size = new Size(26, 26),
                TextAlign = ContentAlignment.MiddleCenter,
            });
            card.Controls.Add(badge);
            var ty = AddText(card, p.Title, Theme.Text(9f, FontStyle.Bold), p.Color, 52, cy + 1, cardW - 52 - 14);
            ty = AddText(card, p.Text, Theme.Text(8.75f), Theme.Ink, 52, ty, cardW - 52 - 14);
            cy = Math.Max(ty, cy + 26) + 12;
        }
        card.Size = new Size(cardW, cy + 2);
        Controls.Add(card);
        y += points.Count > 0 ? card.Height + 18 : 6;

        // Botones: el principal en oro a la derecha, el otro a su izquierda.
        var okButton = new PillButton
        {
            Text = ok,
            Size = new Size(130, 36),
            Location = new Point(W - Pad - 130, y),
            Radius = 11,
            BackColor = Theme.Accent,
            HoverColor = Theme.AccentHover,
            ForeColor = Theme.AccentInk,
            DialogResult = DialogResult.OK,
        };
        var cancelButton = new PillButton
        {
            Text = cancel ?? "",
            Size = new Size(150, 36),
            Location = new Point(W - Pad - 130 - 8 - 150, y),
            Radius = 11,
            DialogResult = DialogResult.No, // distinto de cerrar la ventana (Cancel): ver Ask
            Visible = cancel != null,
        };
        Controls.Add(okButton);
        Controls.Add(cancelButton);
        AcceptButton = okButton;
        CancelButton = cancel != null ? cancelButton : okButton;
        ActiveControl = okButton;
        ClientSize = new Size(W, y + 36 + Pad);
    }

    /// <summary>Muestra el diálogo. True si se pulsa el botón principal; false si se cancela o se cierra.</summary>
    /// <param name="owner">Ventana dueña, o null si aún no hay ninguna (se centra en la pantalla).</param>
    /// <param name="cancel">Texto del segundo botón, o null para mostrar solo el principal.</param>
    public static bool Confirm(IWin32Window? owner, string title, string intro, IReadOnlyList<InfoPoint> points, string ok, string? cancel) =>
        Ask(owner, title, intro, points, ok, cancel) == true;

    /// <summary>Como <see cref="Confirm"/>, pero distingue cerrar la ventana (null) del segundo botón (false).</summary>
    public static bool? Ask(IWin32Window? owner, string title, string intro, IReadOnlyList<InfoPoint> points, string ok, string? cancel)
    {
        using var dialog = new InfoDialog(title, intro, points, ok, cancel, owner != null);
        return (owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog()) switch
        {
            DialogResult.OK => true,
            DialogResult.No => false,
            _ => null,
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    /// <summary>Texto con salto de línea, del alto justo. Devuelve la coordenada de debajo.</summary>
    private int AddText(Control parent, string text, Font font, Color color, int x, int y, int width)
    {
        // Se mide a 96 ppp: el escalado automático de la ventana ya agranda después todo junto.
        var scale = DeviceDpi / 96f;
        var measured = TextRenderer.MeasureText(text, font, new Size((int)(width * scale), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        var height = (int)Math.Ceiling(measured.Height / scale) + 2;
        parent.Controls.Add(new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            UseMnemonic = false,
            Location = new Point(x, y),
            Size = new Size(width, height),
        });
        return y + height;
    }
}
