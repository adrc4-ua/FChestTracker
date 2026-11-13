using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// The "Equipo" card of the main window (with teammates on): the player's chests and those each teammate was seen
/// opening in the current or last match. Teammates' figures are a minimum: the replay only holds what happens nearby.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class TeamCard
{
    private const int Columns = 4; // the player and up to three teammates (squads)

    private readonly SectionTitle _title;
    private readonly FitLabel _total, _message;
    private readonly FitLabel[] _names = new FitLabel[Columns], _values = new FitLabel[Columns];

    public TeamCard(Point at, int width, int height)
    {
        Card = new Card { Location = at, Size = new Size(width, height), Visible = false };
        _title = new SectionTitle { Text = L.T("Equipo", "Team"), Location = new Point(12, 7), Size = new Size(260, 20) };
        Card.Controls.Add(_title);
        _total = FitLabel.Add(Card, "", 7.75f, FontStyle.Regular, Theme.Muted, new Point(width - 14 - 300, 9), new Size(300, 16), ContentAlignment.MiddleRight);
        var column = (width - 28) / Columns;
        for (var i = 0; i < Columns; i++)
        {
            _names[i] = FitLabel.Add(Card, "", 7.75f, FontStyle.Regular, Theme.Muted, new Point(14 + i * column, 32), new Size(column - 12, 14));
            _values[i] = FitLabel.Add(Card, "", 14f, FontStyle.Bold, Theme.Ink, new Point(12 + i * column, 46), new Size(column - 12, 26), figure: true, gold: i == 0);
        }
        _message = FitLabel.Add(Card, "", 8.25f, FontStyle.Regular, Theme.Muted, new Point(14 + column, 32), new Size(width - 28 - column, 40), ContentAlignment.MiddleLeft);
    }

    public Card Card { get; }

    public void Show(TeamSummary team)
    {
        _title.Text = team.Live ? L.T("Equipo · partida actual", "Team · current match") : L.T("Equipo · última partida", "Team · last match");
        _title.ForeColor = team.Live ? Theme.Mint : Theme.Ink;
        _names[0].Text = L.T("Tú", "You");
        _values[0].Text = L.N(team.Mine);

        var message = team.Unknown
            ? L.T("Partida analizada antes de guardar compañeros.", "Match analyzed before teammates were saved.")
            : team.Mates.Count == 0 ? L.T("Partida en solitario: sin compañeros.", "Solo match: no teammates.") : null;
        _message.Text = message ?? "";
        _message.Visible = message != null;
        _total.Text = team.Mates.Count == 0 ? ""
            : team.Total > 0
                ? L.F("Equipo ≥ {0} · compañeros ≥ {1} %", "Team ≥ {0} · teammates ≥ {1} %", L.N(team.Total), (100.0 * team.MatesTotal / team.Total).ToString("0", L.Culture))
                : L.F("Equipo ≥ {0}", "Team ≥ {0}", L.N(team.Total));
        for (var i = 1; i < Columns; i++)
        {
            var mate = message == null && i - 1 < team.Mates.Count ? team.Mates[i - 1] : null;
            _names[i].Text = mate?.Name ?? "";
            _values[i].Text = mate != null ? "≥ " + L.N(mate.Chests) : "";
            _names[i].Visible = _values[i].Visible = message == null;
        }
    }
}
