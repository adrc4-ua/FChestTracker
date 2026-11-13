using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// The icon in the notification area: the app stays there while its window is hidden. A left click opens the window;
/// the right click shows the menu.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;

    public TrayIcon(Icon? icon, Action open, Action overlay, Action stats, Action settings, Action about, Action exit)
    {
        var menu = new ContextMenuStrip { Renderer = new DarkMenuRenderer(), ForeColor = Theme.Ink, Font = Theme.Text(9f) };
        var openItem = new ToolStripMenuItem(L.F("Abrir {0}", "Open {0}", AppInfo.ShortName), null, (_, _) => open());
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);
        menu.Items.Add(openItem);
        menu.Items.Add(Item(L.T("Overlay", "Overlay"), overlay));
        menu.Items.Add(Item(L.T("Estadísticas", "Stats"), stats));
        menu.Items.Add(Item(L.T("Configuración", "Settings"), settings));
        menu.Items.Add(Item(L.T("Acerca de y licencia", "About and license"), about));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Item(L.T("Salir", "Exit"), exit));
        foreach (ToolStripItem item in menu.Items)
        {
            item.ForeColor = Theme.Ink;
        }

        _icon = new NotifyIcon { Icon = icon ?? SystemIcons.Application, Text = AppInfo.Name, ContextMenuStrip = menu, Visible = true };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                open();
            }
        };
    }

    private static ToolStripMenuItem Item(string text, Action action) => new(text, null, (_, _) => action());

    /// <summary>Hidden first: otherwise the icon lingers in the tray until the mouse passes over it.</summary>
    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
