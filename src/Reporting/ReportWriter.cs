using System.Globalization;
using System.Net;
using System.Text;

namespace FCT.Reporting;

public static class ReportWriter
{
    /// <summary>Set while several replays are being analyzed: the report shows a loading card and refreshes faster.</summary>
    public static volatile ScanProgress? Progress;

    private static CultureInfo Loc => L.Culture;

    public static void WriteAll(Store store, Config config, TrackerData data)
    {
        try
        {
            var page = config.ForReport();
            using (L.Scope(config.Language))
            {
                WriteAtomic(store.ReportPath, Report(page, Profiles.View(data, page), Profiles.List(data, page), Profiles.InitialChests(data, page)));
                if (File.Exists(store.OldReportPath))
                {
                    try { File.Delete(store.OldReportPath); } catch { /* abierto en el navegador: la próxima vez */ }
                }
                FCT.Overlay.ObsServer.ReportLang = L.Code;
            }
            var view = Profiles.View(data, config);
            WriteAtomic(store.OverlayPath, Overlay(OverlayModel.Build(view, null, false, Profiles.InitialChests(data, config))));
            WriteAtomic(store.CsvPath, Csv(data));
        }
        catch (Exception ex)
        {
            Console.WriteLine(L.F("No se pudo escribir el resumen: {0}", "Cannot write the summary: {0}", ex.Message));
        }
    }

    private static void WriteAtomic(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content, new UTF8Encoding(false));
        File.Move(tmp, path, overwrite: true);
    }

    private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static DateTime When(MatchRecord m) => (m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime();

    // Paleta negro + oro, solo oscura.
    private const string Style = """
        :root{color-scheme:dark;--bg:#08080a;--bg2:#111114;--card:#121215;--card2:#17171b;--ink:#f3eee3;--muted:#968f80;--faint:#5a554b;--line:#1f1e1d;--line2:#2c2a26;
          --gold:#e8b04b;--gold-hi:#f8d98f;--gold-lo:#8a6524;--gold-soft:rgba(232,176,75,.10);--gold-line:rgba(232,176,75,.30);
          --champ:#d8c28f;--champ-soft:rgba(216,194,143,.10);
          --t1:#e8b04b;--t2:#f8d98f;--t3:#c48a2c;--t4:#d8c28f;--t5:#a9853f;
          --radius:18px;--shadow:0 1px 0 rgba(255,255,255,.03) inset,0 18px 40px -18px rgba(0,0,0,.7)}
        *{box-sizing:border-box}
        html{background:var(--bg)}
        body{margin:0;color:var(--ink);font:14px/1.55 "Segoe UI Variable Text","Segoe UI",system-ui,-apple-system,sans-serif;-webkit-font-smoothing:antialiased;
          background:radial-gradient(900px 420px at 50% -160px,rgba(232,176,75,.17),transparent 70%),radial-gradient(700px 520px at 105% 105%,rgba(232,176,75,.08),transparent 70%),var(--bg);background-attachment:fixed;min-height:100vh}
        [hidden]{display:none!important}
        svg .f{fill:currentColor;fill-opacity:.22}
        .num{font-variant-numeric:tabular-nums}
        .muted{color:var(--muted)}
        .gold{color:var(--gold)}

        /* Estructura: barra lateral con el menú y la sección abierta a la derecha */
        .app{display:grid;grid-template-columns:236px minmax(0,1fr);min-height:100vh}
        .side{position:sticky;top:0;height:100vh;display:flex;flex-direction:column;gap:26px;padding:24px 14px;border-right:1px solid var(--line2);
          background:linear-gradient(180deg,rgba(232,176,75,.06),transparent 45%),#0b0b0d}
        .brand{display:flex;align-items:center;gap:12px;padding:0 6px}
        .brand img{width:52px;height:52px;flex:none;filter:drop-shadow(0 6px 14px rgba(232,176,75,.35))}
        .brand .bl{width:44px;height:44px;border-radius:12px;display:grid;place-items:center;color:var(--gold);background:var(--gold-soft)}
        .bn{display:flex;flex-direction:column;line-height:1.2;min-width:0}
        .bn span{font-size:10.5px;font-weight:700;letter-spacing:.18em;text-transform:uppercase;color:var(--gold)}
        .bn b{font-size:16px;letter-spacing:-.01em;background:linear-gradient(90deg,var(--gold-hi),var(--gold));-webkit-background-clip:text;background-clip:text;color:transparent}
        .main{min-width:0;padding:26px 30px 34px}
        .ph{display:flex;align-items:center;gap:14px;margin:0 0 20px;padding-bottom:16px;border-bottom:1px solid var(--line2)}
        .phi{width:42px;height:42px;flex:none;border-radius:12px;display:grid;place-items:center;color:#1a1206;background:linear-gradient(180deg,var(--gold-hi),var(--gold));box-shadow:0 8px 22px -8px rgba(232,176,75,.7)}
        .phi svg .f{fill-opacity:.35}
        .phi svg{width:22px;height:22px}
        .ph h1{margin:0;font-size:24px;line-height:1.2;font-weight:700;letter-spacing:-.02em;background:linear-gradient(180deg,#fff7e4,var(--gold-hi));-webkit-background-clip:text;background-clip:text;color:transparent}
        .ph p{margin:3px 0 0;font-size:13px;color:var(--muted)}
        .ph p::first-letter{text-transform:uppercase}

        /* Cifras clave */
        .kpis{display:grid;grid-template-columns:minmax(0,1.9fr) repeat(5,minmax(0,1fr));gap:12px}
        .kpi,.card,.mini-kpi{background:linear-gradient(180deg,rgba(232,176,75,.05),rgba(232,176,75,0) 55%),var(--card);border:1px solid rgba(232,176,75,.14);border-radius:var(--radius);box-shadow:var(--shadow)}
        .kpi{position:relative;padding:16px 18px;min-width:0;display:flex;flex-direction:column}
        .kpi .ki{display:flex;align-items:center;justify-content:center;width:36px;height:36px;border-radius:11px;color:var(--gold-hi);background:linear-gradient(180deg,rgba(232,176,75,.22),rgba(232,176,75,.07));border:1px solid var(--gold-line);position:absolute;top:14px;right:14px}
        .kpi .l{padding-right:40px}
        .kpi .l{font-size:12.5px;font-weight:600;color:var(--muted)}
        .kpi .v{margin-top:2px;font-size:28px;font-weight:700;line-height:1.15;letter-spacing:-.025em;font-variant-numeric:tabular-nums;overflow-wrap:anywhere}
        .kpi .v,.mini-kpi b{background:linear-gradient(180deg,var(--gold-hi),var(--gold) 75%);-webkit-background-clip:text;background-clip:text;color:transparent}
        .kpi .s{margin-top:4px;font-size:12px;color:var(--muted)}
        .kpi.hero{flex-direction:row;align-items:center;gap:18px;overflow:hidden;border-color:var(--gold-line);
          background:radial-gradient(420px 220px at 0% 0%,rgba(232,176,75,.17),transparent 70%),var(--card)}
        .kpi.hero img{width:88px;height:88px;flex:none;filter:drop-shadow(0 8px 20px rgba(232,176,75,.35))}
        .kpi.hero .l{font-size:13px;color:var(--ink)}
        .kpi.hero .v{font-size:44px;background:linear-gradient(180deg,var(--gold-hi),var(--gold) 60%,#c48a2c);-webkit-background-clip:text;background-clip:text;color:transparent}
        .kpi.hero .today b{color:var(--ink);font-weight:600}

        /* Secciones */
        section{margin-top:22px}
        .sh{display:flex;flex-wrap:wrap;align-items:baseline;justify-content:space-between;gap:4px 16px;margin:0 0 10px}
        h2{margin:0;font-size:15px;font-weight:700;letter-spacing:.01em}
        h2::before{content:"";display:inline-block;width:6px;height:6px;border-radius:2px;margin:0 10px 2px 0;background:var(--gold);transform:rotate(45deg)}
        .sh .meta{font-size:12.5px;color:var(--muted)}
        .pad{padding:16px 18px}
        .grid2{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px 16px;margin-top:0}
        .grid2 section{margin-top:0}
        .grid2>section,.trow>section{display:flex;flex-direction:column}
        .grid2>section>.card,.trow>section>.card{flex:1 1 auto}

        /* Evolución de cofres: curva a todo el ancho, textos y puntos encima */
        .evo{position:relative;height:clamp(120px,19.6vh,240px);margin:6px 8px 26px 44px}
        .evo svg{position:absolute;inset:0;width:100%;height:100%;overflow:visible}
        .evo .area{fill:url(#evoFill)}
        .evo .line{fill:none;stroke:var(--gold);stroke-width:2.5;stroke-linejoin:round;stroke-linecap:round;vector-effect:non-scaling-stroke;filter:drop-shadow(0 0 6px rgba(232,176,75,.45))}
        .evo .gl{position:absolute;left:0;right:0;height:1px;background:var(--line2)}
        .evo .ya{position:absolute;right:calc(100% + 10px);transform:translateY(-50%);font-size:11.5px;color:var(--faint);font-variant-numeric:tabular-nums}
        .evo .xa{position:absolute;top:calc(100% + 8px);transform:translateX(-50%);font-size:11.5px;color:var(--faint);white-space:nowrap}
        .evo .xa.now{color:var(--gold);font-weight:600}
        .evo .al{position:absolute;left:0;right:0;border-top:1px dashed rgba(248,217,143,.45);z-index:1}
        .evo .al i{position:absolute;left:6px;bottom:4px;font-style:normal;font-size:11.5px;font-weight:600;color:var(--gold-hi)}
        .evo .pt{position:absolute;width:10px;height:10px;margin:-5px 0 0 -5px;border-radius:50%;background:var(--gold-hi);border:2px solid var(--card);z-index:2;cursor:default}
        .evo .pt.now{background:#fff}
        .evo .pt:hover{transform:scale(1.4)}

        /* Resumen: últimas partidas y columna derecha */
        .duo{display:grid;grid-template-columns:minmax(0,1.75fr) minmax(280px,1fr);gap:16px;margin-top:22px}
        #t-resumen .duo .sh{min-height:28px;align-items:center}
        #t-resumen .duo .sh .more{white-space:nowrap;flex:none}
        .duo>section,.stack>section{margin-top:0}
        .stack{display:flex;flex-direction:column;gap:16px;min-width:0}
        .side-card{margin:0;padding:14px 18px}
        .empty-side{margin:6px 0;text-align:center}
        .tl{list-style:none;margin:0;padding:0}
        .tl li{display:flex;align-items:center;gap:12px;padding:8px 0;border-top:1px solid var(--line)}
        .tl li:first-child{border-top:0;padding-top:2px}
        .gem{width:36px;height:36px;flex:none;border-radius:11px;display:grid;place-items:center;color:var(--gold);background:var(--bg2);border:1px solid rgba(232,176,75,.2)}
        .gem.f{color:var(--gold-hi);background:var(--gold-soft);border-color:var(--gold-line)}
        .tn{display:flex;flex-direction:column;min-width:0}
        .tn b{font-size:13.5px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .tn small{font-size:12px;color:var(--muted);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .teamstrip{margin-top:16px}
        .team2{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr) auto;align-items:center;gap:16px;padding:11px 18px;color:inherit;text-decoration:none;transition:border-color .15s}
        .team2:hover{border-color:var(--gold-line)} .team2:focus-visible{outline:2px solid var(--gold);outline-offset:2px}
        .team2 .go{color:var(--gold);font-size:18px;font-weight:700}
        .team2>div{display:flex;flex-direction:column;gap:4px;min-width:0}
        .team2>div+div{padding-left:16px;border-left:1px solid var(--line2)}
        .team2 b{font-size:20px;line-height:1.15;color:var(--gold)}
        .team2 b.nm{font-size:15px;color:var(--ink);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .team2 .track{display:block;height:5px;margin-top:4px}
        .gemc{display:inline-flex;align-items:center;gap:5px;color:var(--gold);font-weight:600}
        table.compact th,table.compact td{padding:9px 14px}
        .trow{display:grid;grid-template-columns:minmax(0,1.6fr) minmax(0,1fr);gap:16px;align-items:stretch}
        .trow>section{margin-top:22px}
        .trow .tiles{grid-template-columns:repeat(auto-fit,minmax(104px,1fr))}

        /* Listas con barra de proporción */
        .rows{display:flex;flex-direction:column}
        .row{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:6px 12px;padding:11px 0;border-bottom:1px solid var(--line)}
        .row:last-child{border-bottom:none}
        .row .n{font-weight:700;font-variant-numeric:tabular-nums;text-align:right}
        .row .n small{font-weight:500;color:var(--muted);margin-left:6px}
        .track{grid-column:1/-1;height:5px;border-radius:3px;background:var(--bg2);overflow:hidden}
        .track i{display:block;height:100%;border-radius:3px;background:linear-gradient(90deg,var(--gold-lo),var(--gold))}
        .row.sub{color:var(--muted)} .row.sub .n{color:var(--muted);font-weight:600}
        .row.warn .track i{background:linear-gradient(90deg,#6b5a3a,#a89064)}

        /* Tablas */
        .tw{overflow-x:auto}
        table{width:100%;border-collapse:collapse;font-variant-numeric:tabular-nums}
        th,td{text-align:left;padding:12px 16px;border-bottom:1px solid var(--line);vertical-align:middle}
        th{position:sticky;top:0;z-index:1;background:linear-gradient(180deg,#1c1913,#18161a);font-size:11px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;color:#bf9a5c;white-space:nowrap}
        thead tr th:first-child{border-top-left-radius:17px} thead tr th:last-child{border-top-right-radius:17px}
        tbody tr:last-child td{border-bottom:none}
        tbody tr{transition:background .12s}
        tbody tr:hover td{background:rgba(232,176,75,.035)}
        .c{text-align:center} .nw{white-space:nowrap}
        td.c .chests{justify-content:center} th.c button{justify-content:center}
        .sm{font-size:12.5px;color:var(--muted)}
        .badge{display:inline-block;line-height:1.5;padding:2px 10px;border-radius:99px;font-size:12.5px;font-weight:600;white-space:nowrap;background:var(--bg2);color:var(--muted);border:1px solid var(--line2)}
        .badge.win{background:var(--gold-soft);color:var(--gold-hi);border-color:var(--gold-line)}
        .badge.top{background:var(--champ-soft);color:var(--champ);border-color:rgba(216,194,143,.25)}
        .badge.mode{background:transparent;border:1px solid rgba(232,176,75,.28);color:var(--ink);font-weight:500}
        .chests{display:flex;align-items:center;justify-content:flex-end;gap:10px}
        .chests .mini{width:70px;height:5px;border-radius:3px;background:var(--bg2);overflow:hidden}
        .chests .mini i{display:block;height:100%;background:linear-gradient(90deg,var(--gold-lo),var(--gold));border-radius:3px}
        .chests b{min-width:28px;text-align:right}
        .star{color:var(--gold);font-weight:600;letter-spacing:-.05em}
        .tlist{display:flex;flex-direction:column;gap:2px}
        .tlist .star{letter-spacing:0;white-space:nowrap}
        .tname{display:flex;gap:8px;align-items:baseline;font-weight:600}
        .tags{display:flex;flex-wrap:wrap;gap:6px;margin-top:6px}
        .tag{display:inline-flex;align-items:center;padding:1px 8px;border-radius:99px;font-size:11.5px;font-weight:600;white-space:nowrap;background:var(--bg2);color:var(--muted);border:1px solid var(--line2)}
        .tag.gold{color:var(--gold-hi);background:var(--gold-soft);border-color:var(--gold-line)}
        tr.unusual td{background:rgba(232,176,75,.05)}
        .flag{display:flex;gap:12px;align-items:flex-start;padding:14px 18px;border:1px solid var(--gold-line);border-radius:14px;background:var(--gold-soft)}
        .flag .ic{color:var(--gold);font-size:18px;line-height:1.2}
        .empty{padding:28px 20px;text-align:center;color:var(--muted)}

        /* Caza del tesoro (resumen) */
        .hunt{padding:18px 20px}
        .tiles{display:grid;grid-template-columns:repeat(auto-fill,minmax(170px,1fr));gap:10px}
        .tile{position:relative;display:flex;flex-direction:column;gap:3px;padding:12px 14px;border-radius:14px;background:var(--bg2);border:1px solid var(--line2);min-width:0}
        .tile .tt{font-size:10.5px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;color:var(--muted)}
        .tile b{font-size:14px;line-height:1.3;overflow-wrap:anywhere}
        .tile .tag{align-self:flex-start;margin-top:4px}
        .tile.got{border-color:var(--gold-line);background:radial-gradient(220px 120px at 100% 0%,rgba(232,176,75,.16),transparent 70%),var(--bg2)}
        .tile.got b{color:var(--gold-hi)}
        .tile .won{align-self:flex-start;margin-top:4px;font-size:11.5px;font-weight:700;letter-spacing:0;color:#1a1305;background:var(--gold);border-radius:99px;padding:0 7px}
        .hunt-note{margin:0}
        .always{display:flex;flex-wrap:wrap;align-items:center;gap:6px 8px;margin-top:14px;padding-top:14px;border-top:1px solid var(--line)}
        .always .al{font-size:11px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;color:var(--muted);margin-right:4px}
        .pchip{display:inline-flex;align-items:center;gap:5px;padding:3px 10px;border-radius:99px;font-size:12.5px;background:var(--bg2);border:1px solid var(--line2);color:var(--ink);white-space:nowrap}
        .pchip.got{color:var(--gold-hi);background:var(--gold-soft);border-color:var(--gold-line)}

        /* Las cinco semanas, en una fila */
        .weeks{margin-top:16px}
        .wgrid{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:10px}
        .wk{padding:12px 14px;border-radius:14px;background:var(--card);border:1px solid rgba(232,176,75,.14);min-width:0}
        .wk.now{border-color:var(--gold-line);background:radial-gradient(240px 120px at 100% 0%,rgba(232,176,75,.14),transparent 70%),var(--card)}
        .wh{display:flex;flex-wrap:wrap;align-items:center;gap:2px 8px;margin-bottom:8px}
        .wh b{font-size:13.5px} .wh .wr{flex-basis:100%;font-size:11.5px;color:var(--muted)}
        .wk ul{list-style:none;margin:0;padding:0;display:flex;flex-direction:column;gap:7px}
        .wk li{display:grid;grid-template-columns:18px minmax(0,1fr) auto;align-items:center;gap:8px;font-size:12.5px}
        .wk li i{display:flex;color:var(--gold)} .wk li i svg{width:16px;height:16px}
        .wk li span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
        .wk li.got span{color:var(--gold-hi);font-weight:600} .wk li b{color:var(--gold);font-size:12px}
        .wk p.sm{margin:2px 0}
        .wtoggle{appearance:none;font:inherit;font-size:13px;font-weight:600;color:var(--gold-hi);background:var(--gold-soft);border:1px solid var(--gold-line);border-radius:10px;padding:6px 14px;margin-top:14px;cursor:pointer}
        .wtoggle::after{content:" ▾"} .wtoggle[aria-expanded="true"]::after{content:" ▴"}
        .wtoggle:hover{background:rgba(232,176,75,.18)} .wtoggle:focus-visible{outline:2px solid var(--gold-hi);outline-offset:2px}
        .tile .tt{display:flex;align-items:flex-start;gap:6px;text-transform:none;letter-spacing:0;font-size:11.5px;font-weight:600;color:var(--gold)} .tile .tt>span{min-width:0} .tile .tt i{display:flex;color:var(--gold)} .tile .tt i svg{width:15px;height:15px}
        .pchip i{display:flex;color:var(--gold)} .pchip i svg{width:14px;height:14px}
        @media (max-width:1100px){.wgrid{grid-template-columns:repeat(3,minmax(0,1fr))}}
        @media (max-width:600px){.wgrid{grid-template-columns:repeat(2,minmax(0,1fr))}}

        /* Clasificaciones cortas y tabla que se ordena */
        .rank.one{padding:2px 18px}
        .duo.wide{grid-template-columns:minmax(0,2.6fr) minmax(250px,1fr)}
        table.sortable th,table.sortable td{padding:12px 10px} table.sortable th{white-space:normal;line-height:1.3} table.sortable th:first-child,table.sortable td:first-child{padding-left:18px}
        table.sortable td .nm{max-width:170px}
        .rank.one ol{grid-auto-flow:row;grid-template-columns:1fr}
        .rank.one li{padding:8px 0 9px}
        th button{all:unset;cursor:pointer;display:inline-flex;align-items:center;gap:4px}
        th button:focus-visible{outline:2px solid var(--gold);outline-offset:3px;border-radius:4px}
        th[aria-sort]{color:var(--gold-hi)}
        th[aria-sort="descending"] button::after{content:"▾"} th[aria-sort="ascending"] button::after{content:"▴"}
        tfoot td{border-bottom:0;border-top:1px solid var(--line)}

        /* Ajustes */
        .abouthero{display:flex;align-items:center;gap:20px;padding:18px 22px;margin-bottom:22px;border-color:var(--gold-line);
          background:radial-gradient(520px 220px at 0% 0%,rgba(232,176,75,.18),transparent 70%),var(--card)}
        .abouthero img{flex:none;filter:drop-shadow(0 8px 20px rgba(232,176,75,.4))}
        .abouthero .ai{display:flex;flex-direction:column;gap:2px;min-width:0;flex:1}
        .abouthero .ai b{font-size:20px;letter-spacing:-.01em;background:linear-gradient(180deg,#fff7e4,var(--gold-hi));-webkit-background-clip:text;background-clip:text;color:transparent}
        .abouthero .ver{font-size:13.5px;font-weight:700;color:var(--gold)} .abouthero small{font-size:12.5px;color:var(--muted)}
        .abouthero .links{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:8px}
        .abouthero .links a{color:var(--gold-hi);text-decoration:none;font-size:13px;font-weight:600;padding:7px 13px;border-radius:10px;border:1px solid var(--gold-line);background:var(--gold-soft);white-space:nowrap}
        .abouthero .links a:hover{background:rgba(232,176,75,.2)} .abouthero .links a:focus-visible{outline:2px solid var(--gold-hi);outline-offset:2px}
        .setgrid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:0 16px;align-items:stretch}
        .setgrid>section{margin-top:0;display:flex;flex-direction:column} .setgrid>section>.card{flex:1}
        .si{flex:none;display:grid;place-items:center;width:36px;height:36px;border-radius:11px;color:var(--gold-hi);background:linear-gradient(180deg,rgba(232,176,75,.22),rgba(232,176,75,.07));border:1px solid var(--gold-line)}
        .setrow .st{flex:1}
        .setrow.col{flex-direction:column;align-items:center;text-align:center;gap:12px;padding:18px 0}
        .setrow.col .st{flex:none;width:100%}
        .setrow.col .sel{width:100%;max-width:260px;padding:9px 36px 9px 14px;text-align:center;text-align-last:center}
        @media (max-width:1250px){.setgrid{grid-template-columns:repeat(2,minmax(0,1fr))}.setgrid>section:nth-child(3){margin-top:18px}}
        .set{padding:2px 20px;display:flex;flex-direction:column;justify-content:center}
        .setrow{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:13px 0;border-top:1px solid var(--line)}
        .setrow:first-child{border-top:0}
        .setrow .st{min-width:0} .setrow .st b{display:block;font-weight:600} .setrow .st small{display:block;font-size:12.5px;color:var(--muted)}
        .sw{position:relative;display:inline-block;width:44px;height:24px;flex:none}
        .sw input{position:absolute;inset:0;width:100%;height:100%;margin:0;opacity:0;cursor:pointer;z-index:1}
        .sw span{position:absolute;inset:0;border-radius:99px;background:var(--line2);transition:background .15s}
        .sw span::after{content:"";position:absolute;top:3px;left:3px;width:18px;height:18px;border-radius:50%;background:var(--muted);transition:left .15s,background .15s}
        .sw input:checked+span{background:linear-gradient(180deg,var(--gold-hi),var(--gold))}
        .sw input:checked+span::after{left:23px;background:#1a1206}
        .sw input:focus-visible+span{outline:2px solid var(--gold-hi);outline-offset:2px}
        .sw input:disabled{cursor:not-allowed} .sw input:disabled+span{opacity:.45}
        .sel{appearance:none;font:inherit;font-size:13.5px;font-weight:600;color:var(--ink);background:var(--bg2);border:1px solid var(--gold-line);border-radius:10px;padding:7px 34px 7px 12px;cursor:pointer;transition:border-color .15s,background-color .15s;
          background-image:linear-gradient(45deg,transparent 50%,var(--gold) 50%),linear-gradient(135deg,var(--gold) 50%,transparent 50%);background-position:calc(100% - 17px) 52%,calc(100% - 12px) 52%;background-size:5px 5px;background-repeat:no-repeat}
        .sel:hover:not(:disabled){border-color:var(--gold);background-color:rgba(232,176,75,.07)}
        .sel:focus-visible{outline:2px solid var(--gold-hi);outline-offset:2px} .sel:disabled{opacity:.5;cursor:not-allowed}
        .sel option{background:#151417;color:var(--ink)}
        .segc{display:inline-flex;flex:none;gap:3px;padding:3px;border:1px solid var(--line2);border-radius:10px;background:var(--bg2)}
        .segc label{position:relative}
        .segc input{position:absolute;inset:0;margin:0;opacity:0;cursor:pointer}
        .segc span{display:block;padding:5px 11px;border-radius:7px;font-size:12.5px;font-weight:600;color:var(--muted);white-space:nowrap}
        .segc input:checked+span{color:#1a1206;background:linear-gradient(180deg,var(--gold-hi),var(--gold))}
        .segc input:focus-visible+span{outline:2px solid var(--gold-hi);outline-offset:1px} .segc input:disabled{cursor:not-allowed}
        .toast{position:fixed;right:24px;bottom:24px;z-index:50;padding:10px 16px;border-radius:12px;background:#1d1a12;border:1px solid var(--gold-line);color:var(--gold-hi);font-weight:600;box-shadow:var(--shadow)}
        .toast.bad{color:#f2a58f;border-color:rgba(242,120,90,.45)}
        .offline,.offline-note{margin:0 0 16px;padding:10px 14px;border-radius:12px;border:1px solid var(--gold-line);background:var(--gold-soft);color:var(--gold-hi);font-size:13px}
        .offline-note a{color:var(--gold-hi);font-weight:600}
        @media (max-width:860px){.setgrid{grid-template-columns:1fr}.setgrid>section+section{margin-top:18px}.abouthero{flex-wrap:wrap}.abouthero .links{justify-content:flex-start}}

        /* Carga */
        .loading{display:flex;gap:16px;align-items:center;background:var(--card);border:1px solid var(--gold-line);border-radius:var(--radius);padding:16px 18px;margin-bottom:22px;box-shadow:var(--shadow)}
        .spin{width:30px;height:30px;flex:none;border:3px solid var(--line2);border-top-color:var(--gold);border-radius:50%;animation:spin .8s linear infinite}
        @keyframes spin{to{transform:rotate(360deg)}}
        .loading .grow{flex:1;min-width:0}
        .pbar{height:6px;background:var(--line2);border-radius:4px;overflow:hidden;margin:8px 0 6px}
        .pbar i{display:block;height:100%;background:linear-gradient(90deg,var(--gold-lo),var(--gold-hi));border-radius:4px}


        /* Menú lateral: sin JavaScript se ven todas las secciones seguidas */
        .nav{display:flex;flex-direction:column;gap:4px;flex:1}
        .tab.end{margin-top:auto}
        html:not(.js) .nav{display:none}
        .tab{appearance:none;border:0;background:transparent;color:var(--muted);font:inherit;font-size:14.5px;font-weight:600;min-height:44px;width:100%;padding:10px 14px;border-radius:12px;
          cursor:pointer;display:flex;align-items:center;gap:12px;text-align:left;white-space:nowrap;transition:background .15s,color .15s}
        .tab .ti{flex:none;color:var(--gold);opacity:.7}
        .tab:hover{color:var(--ink);background:rgba(255,255,255,.04)}
        .tab[aria-selected="true"]{color:var(--gold-hi);background:linear-gradient(90deg,rgba(232,176,75,.2),rgba(232,176,75,.06));box-shadow:inset 3px 0 0 var(--gold)}
        .tab[aria-selected="true"] .ti{opacity:1}
        .tab .c{margin-left:auto;font-size:11.5px;font-weight:700;padding:0 8px;border-radius:99px;background:var(--bg2);color:var(--muted);font-variant-numeric:tabular-nums}
        .tab[aria-selected="true"] .c{background:var(--gold-soft);color:var(--gold-hi)}
        .tab:focus-visible,.more:focus-visible{outline:2px solid var(--gold);outline-offset:2px}
        .panel>:first-child,.ph+section,.ph+div{margin-top:0}
        html:not(.js) .panel+.panel{margin-top:40px}
        .panel[hidden]{display:none}
        .more{font-size:13px;font-weight:600;color:var(--gold);text-decoration:none;padding:4px 10px;border-radius:8px}
        .more:hover{background:var(--gold-soft)}
        /* Barras de desplazamiento finas (las tarjetas que se desplazan por dentro) */
        *{scrollbar-width:thin;scrollbar-color:#5c4417 transparent}
        ::-webkit-scrollbar{width:8px;height:8px} ::-webkit-scrollbar-thumb{background:#5c4417;border-radius:8px} ::-webkit-scrollbar-track{background:transparent}

        /* Piezas comunes */
        .split b.acc{color:var(--gold-hi)} .split b.mnt{color:var(--champ)}
        .row.rose .track i{background:linear-gradient(90deg,var(--gold-lo),var(--gold))}
        .row.off .track i{background:var(--line2)}
        .row .lbl{display:flex;flex-wrap:wrap;align-items:center;gap:6px 10px}
        .row .badge{font-size:11.5px;padding:1px 8px}
        .minis{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px}
        .mini-kpi{padding:12px 16px;min-width:0}
        .mini-kpi b{display:block;font-size:23px;line-height:1.15;font-variant-numeric:tabular-nums;letter-spacing:-.02em}
        .mini-kpi span{display:block;font-size:12.5px;color:var(--muted)}
        .ros,.acc,.cy{color:var(--gold)}
        .flag{margin:0 0 20px}
        .empty.big{padding:56px 24px;font-size:14.5px}
        .empty.big .star{display:block;font-size:30px;margin-bottom:8px}
        table.matches tr.won td{background:rgba(232,176,75,.06)} table.matches tr.won td:first-child{box-shadow:inset 3px 0 0 var(--gold)}
        .team{margin-top:4px;max-width:340px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
        td.foot{text-align:center;padding:14px 16px}
        .who{display:flex;align-items:center;gap:12px;min-width:0}
        .av{width:40px;height:40px;flex:none;border-radius:50%;display:grid;place-items:center;font-weight:700;font-size:17px;color:#1a1305}
        .av.sm-av{width:26px;height:26px;font-size:12px}
        .who .nm{font-weight:700;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
        .rank{padding:6px 24px}
        .rank ol{list-style:none;margin:0;padding:0;display:grid;grid-auto-flow:column;grid-template-columns:repeat(2,minmax(0,1fr));column-gap:56px}
        .rank li{display:grid;grid-template-columns:24px 28px minmax(0,1fr) auto;align-items:center;gap:4px 12px;padding:11px 0 12px;border-top:1px solid var(--line)}
        .rank li.t{border-top:0}
        @media (max-width:860px){.rank{padding:4px 16px}.rank ol{grid-auto-flow:row;grid-template-columns:1fr;grid-template-rows:none!important}.rank li.t{border-top:1px solid var(--line)}.rank li:first-child{border-top:0}}
        .rank .rk{font-size:12px;font-weight:700;color:var(--faint);text-align:center;font-variant-numeric:tabular-nums}
        .rank .rk.medal{font-size:16px}
        .rank .who2{display:flex;flex-direction:column;min-width:0}
        .rank .who2 .nm{font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
        .rank .who2 .sub{font-size:11.5px;color:var(--muted);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .rank li b{font-size:15px;text-align:right;font-variant-numeric:tabular-nums;white-space:nowrap}
        .rank li b small{font-size:11px;font-weight:600;color:var(--muted);margin-right:2px}
        .rank li.first b{color:var(--gold-hi);font-size:17px}
        .rank .bar{grid-column:3/-1;height:4px;border-radius:2px;background:var(--bg2);overflow:hidden;box-shadow:none}
        .rank .bar i{display:block;height:100%;background:linear-gradient(90deg,var(--gold-lo),var(--gold));border-radius:2px}
        .rank li.first .bar i{background:linear-gradient(90deg,var(--gold),var(--gold-hi))}
        .mates-line{display:flex;flex-wrap:wrap;gap:4px 6px;margin-top:5px}
        .chip{display:inline-flex;align-items:center;gap:5px;padding:2px 9px 2px 3px;border-radius:99px;background:var(--bg2);border:1px solid var(--line);font-size:12px;white-space:nowrap;max-width:100%}
        .matches .chip{padding-left:9px}
        .chip .cn{color:var(--muted);overflow:hidden;text-overflow:ellipsis;max-width:130px}
        .chip b{font-variant-numeric:tabular-nums}
        .chip .sm-av{width:20px;height:20px;font-size:10.5px}
        .mini-kpi b small{font-size:13px;font-weight:600;color:var(--muted);margin-right:2px}
        @media (max-width:860px){.minis{grid-template-columns:repeat(2,minmax(0,1fr))}.grid2,.trow{grid-template-columns:1fr}}
        @media (max-width:1280px){.kpis{grid-template-columns:repeat(5,minmax(0,1fr))}.kpi.hero{grid-column:1/-1}}
        @media (max-width:1100px){.duo{grid-template-columns:1fr}}
        /* Móvil y ventanas estrechas: el menú pasa arriba y la página se desplaza normalmente */
        @media (max-width:999px){.app{grid-template-columns:1fr}.side{position:sticky;z-index:20;height:auto;flex-direction:row;align-items:center;gap:10px;padding:10px 12px;
          border-right:0;border-bottom:1px solid var(--line2);background:rgba(11,11,13,.92);-webkit-backdrop-filter:blur(12px);backdrop-filter:blur(12px)}
          .brand img{width:36px;height:36px}.bn{display:none}.nav{flex-direction:row;overflow-x:auto;scrollbar-width:none}.tab.end{margin-top:0}.nav::-webkit-scrollbar{display:none}
          .tab{width:auto;min-height:40px;padding:8px 12px}.tab[aria-selected="true"]{box-shadow:inset 0 -2px 0 var(--gold)}.main{padding:18px 16px 30px}
          .kpis{grid-template-columns:repeat(2,minmax(0,1fr))}}
        @media (max-width:520px){.kpi.hero img{width:64px;height:64px}.kpi.hero .v{font-size:36px}th,td{padding:10px 12px}.chests .mini{display:none}
          .tab span:not(.c){display:none}.tab[aria-selected="true"] span:not(.c){display:inline}}
        /* PC: todo cabe en la pantalla; lo que no cabe se desplaza dentro de su propia tarjeta */
        @media (min-width:1000px) and (min-height:620px){
          html,body{height:100%;overflow:hidden}
          .app{height:100vh}
          .main{height:100vh;overflow:hidden;display:flex;flex-direction:column}
          .main>.panel:not([hidden]){flex:1;min-height:0;display:flex;flex-direction:column}
          .main>.panel>*{flex:0 0 auto}
          .main>.panel>.fill,.main>.panel>.duo{flex:1 1 0;min-height:140px}
          .fill{display:flex;flex-direction:column;min-height:0}
          .fill>.card{flex:0 1 auto;min-height:0;overflow:auto}
          .duo>section{display:flex;flex-direction:column;min-height:0}
          .duo>section>.card{flex:0 1 auto;min-height:0;overflow:auto}
          #t-resumen .duo>section>.card{flex:1 1 auto;display:flex;flex-direction:column}
          #t-resumen .duo>section>.card:has(tbody tr:nth-child(5)) table.compact{min-height:100%}
          .duo>.stack{min-height:0;display:flex;flex-direction:column;gap:12px;overflow:hidden}
          .stack>section.grow{flex:1 1 0;min-height:0;display:flex;flex-direction:column}
          .stack>section.grow>.card{flex:0 1 auto;min-height:0;overflow:auto}
          #t-resumen .stack>section.grow>.card{flex:1 1 auto}
          .stack>.teamstrip{flex:none;margin-top:0}
          .main>.panel:not([hidden]){overflow:auto}
          .main>.offline{flex:none}
          .trow>section>.card{max-height:42vh;overflow:auto}
        }
        .kpi .s::first-letter,.split span::first-letter,.mini-kpi span::first-letter,.sh .meta::first-letter,
        .rank .who2 .sub::first-letter,.row>span::first-letter,.badge::first-letter,
        .chests+.sm::first-letter,td>.sm::first-letter,.tag::first-letter,.pchip::first-letter{text-transform:uppercase}
        @media (prefers-reduced-motion:reduce){.dot,.spin{animation:none}}
        """;

    // Pestañas de estadísticas. Se abre siempre en Resumen; la pestaña elegida y la posición solo se recuerdan en esa
    // pestaña del navegador mientras la página se recarga sola. Sin JavaScript se ven todas las secciones seguidas.
    private const string TabsScript = """
        (function(){var d=document,b=d.body,tabs=[].slice.call(d.querySelectorAll('[data-tab]'));if(!tabs.length)return;d.documentElement.classList.add('js');
        function get(k,s){try{return (s?sessionStorage:localStorage).getItem(k)}catch(e){return null}}
        function put(k,v,s){try{(s?sessionStorage:localStorage).setItem(k,v)}catch(e){}}
        var live=/^https?:$/.test(location.protocol),saving=false;
        function show(id,save){var t=null;tabs.forEach(function(x){var on=x.getAttribute('data-tab')===id;x.setAttribute('aria-selected',on?'true':'false');x.tabIndex=on?0:-1;if(on)t=x});
          if(!t)return false;var bar=t.parentNode;bar.scrollLeft=t.offsetLeft-(bar.clientWidth-t.offsetWidth)/2;[].forEach.call(d.querySelectorAll('.panel'),function(p){p.hidden=p.id!=='t-'+id});if(save)put('cc-tab',id,true);return true}
        var h=location.hash.slice(1);if(!(h&&show(h,true))&&!show(get('cc-tab',true)||'',false))show(tabs[0].getAttribute('data-tab'),false);
        function go(id){if(show(id,true)){window.scrollTo(0,0);try{history.replaceState(null,'','#'+id)}catch(e){}}}
        tabs.forEach(function(x,i){x.addEventListener('click',function(){go(x.getAttribute('data-tab'))});
          x.addEventListener('keydown',function(e){var k=e.key==='ArrowRight'||e.key==='ArrowDown'?1:e.key==='ArrowLeft'||e.key==='ArrowUp'?-1:0;if(!k)return;e.preventDefault();var n=tabs[(i+k+tabs.length)%tabs.length];go(n.getAttribute('data-tab'));n.focus()})});
        [].forEach.call(d.querySelectorAll('[data-go]'),function(a){a.addEventListener('click',function(e){e.preventDefault();go(a.getAttribute('data-go'))})});
        // Semanas de la caza del tesoro: siguen abiertas al recargar.
        var wt=d.querySelector('.wtoggle'),wk=d.getElementById('weeks');
        function weeks(open){if(!wt||!wk)return;wk.hidden=!open;wt.setAttribute('aria-expanded',open?'true':'false');wt.textContent=wt.getAttribute(open?'data-close':'data-open');put('cc-weeks',open?'1':'0',true)}
        if(wt){weeks(get('cc-weeks',true)==='1');wt.addEventListener('click',function(){weeks(wk.hidden)})}
        // Tablas que se ordenan por columna (Compañeros).
        [].forEach.call(d.querySelectorAll('table.sortable'),function(t){var ths=[].slice.call(t.tHead.rows[0].cells),body=t.tBodies[0];
          function sort(i,desc){var rows=[].slice.call(body.rows),num=ths[i].getAttribute('data-type')==='n';
            rows.sort(function(a,c){var x=a.cells[i].getAttribute('data-v'),y=c.cells[i].getAttribute('data-v');var r=num?(+x)-(+y):x.localeCompare(y);return desc?-r:r});
            rows.forEach(function(r){body.appendChild(r)});ths.forEach(function(h,j){if(j===i)h.setAttribute('aria-sort',desc?'descending':'ascending');else h.removeAttribute('aria-sort')});put('cc-sort',i+(desc?'d':'a'),true)}
          ths.forEach(function(h,i){h.querySelector('button').addEventListener('click',function(){var cur=h.getAttribute('aria-sort');sort(i,cur?cur==='ascending':h.getAttribute('data-type')==='n')})});
          var s=get('cc-sort',true);if(s&&ths[parseInt(s,10)])sort(parseInt(s,10),s.slice(-1)==='d')});
        // Ajustes: solo se pueden cambiar cuando las estadísticas las sirve FCT.
        var toast=d.querySelector('.toast'),tt;
        function say(m,bad){if(!toast)return;toast.textContent=m;toast.className='toast'+(bad?' bad':'');toast.hidden=false;clearTimeout(tt);tt=setTimeout(function(){toast.hidden=true},2600)}
        var ctl=[].slice.call(d.querySelectorAll('[data-key]'));
        if(!live){ctl.forEach(function(c){c.disabled=true});var on=d.querySelector('.offline-note');if(on)on.hidden=false}
        ctl.forEach(function(c){c.setAttribute('data-prev',c.type==='checkbox'?(c.checked?'1':'0'):c.value);
          c.addEventListener('change',function(){if(c.type==='radio'&&!c.checked)return;var key=c.getAttribute('data-key'),val=c.type==='checkbox'?(c.checked?'1':'0'):c.value;saving=true;
            function reloadWhenReady(n){if(key!=='lang'||!live){saving=false;location.reload();return}
              fetch('/api/ping',{cache:'no-store'}).then(function(r){if(r.ok){saving=false;location.reload()}else throw 0})
                .catch(function(){if(n>0)setTimeout(function(){reloadWhenReady(n-1)},250);else{saving=false;location.reload()}})}
            fetch('/api/ajuste',{method:'POST',headers:{'X-FCT':'1','Content-Type':'application/x-www-form-urlencoded'},body:encodeURIComponent(key)+'='+encodeURIComponent(val)})
              .then(function(r){return r.json()}).then(function(j){if(!j.ok)throw 0;
                say(b.getAttribute('data-saved'));setTimeout(function(){reloadWhenReady(16)},1000)})
              .catch(function(){saving=false;say(b.getAttribute('data-failed'),true);
                if(c.type==='checkbox')c.checked=c.getAttribute('data-prev')==='1';else if(c.type==='radio'){var p=d.querySelector('[data-key="'+key+'"][value="'+c.getAttribute('data-prev')+'"]');[].forEach.call(d.querySelectorAll('[data-key="'+key+'"]'),function(x){x.checked=x.value===(p?p.value:'')})}else c.value=c.getAttribute('data-prev')})})});
        [].forEach.call(d.querySelectorAll('[data-key="updates"]'),function(x){x.setAttribute('data-prev',(d.querySelector('[data-key="updates"]:checked')||{}).value||'')});
        // Recarga sola; servida por FCT, solo si FCT sigue abierto (si no, un aviso en vez de una página de error).
        // Si cambia el idioma en la app mientras la página está abierta, recarga enseguida sin esperar al intervalo completo.
        var off=d.querySelector('.offline'),docLang=d.documentElement.lang||'',missed=0;
        function busy(){var a=d.activeElement;return saving||(a&&/^(INPUT|SELECT|TEXTAREA)$/.test(a.tagName)&&a.closest('.set'))}
        if(live){setInterval(function(){if(saving)return;
          fetch('/api/ping',{cache:'no-store'}).then(function(r){return r.ok?r.json():Promise.reject()}).then(function(j){
            var wasDown=missed>=3;missed=0;if(off)off.hidden=true;
            if((j.lang&&docLang&&j.lang!==docLang)||wasDown)location.reload();
          }).catch(function(){missed++;if(missed>=3&&off){off.textContent=b.getAttribute('data-offline');off.hidden=false}})},1500)}
        setInterval(function(){if(busy())return;if(!live){location.reload();return}
          fetch('/api/ping',{cache:'no-store'}).then(function(r){if(r.ok)location.reload();else throw 0}).catch(function(){if(off){off.textContent=b.getAttribute('data-offline');off.hidden=false}})},(+b.getAttribute('data-refresh')||15)*1000);
        var y=get('cc-y',true);if(y)window.scrollTo(0,+y);addEventListener('pagehide',function(){put('cc-y',String(window.scrollY),true)});
        })();
        """;

    // Iconos: trazo dorado con un relleno suave (clase "f"), 24 × 24. Todos del mismo dibujo.
    private const string IconSvg = """<svg class="ti" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private const string ChestShape = """<path class="f" d="M4 11V9.5A5.5 5.5 0 0 1 9.5 4h5A5.5 5.5 0 0 1 20 9.5V11z"/><rect x="3" y="11" width="18" height="9" rx="1.6"/><path d="M8.5 4.4V11M15.5 4.4V11M7 11v9M17 11v9"/><rect class="f" x="10" y="9.5" width="4" height="5" rx="1"/>""";
    private const string ChestIcon = """<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""" + ChestShape + "</svg>";
    private static readonly Dictionary<string, string> TabIcons = new()
    {
        ["resumen"] = IconSvg + """<rect class="f" x="3.5" y="3.5" width="7" height="9" rx="2"/><rect x="13.5" y="3.5" width="7" height="5" rx="2"/><rect x="13.5" y="11.5" width="7" height="9" rx="2"/><rect x="3.5" y="15.5" width="7" height="5" rx="2"/></svg>""",
        ["cofres"] = IconSvg + ChestShape + "</svg>",
        ["tesoros"] = IconSvg + """<path class="f" d="M3 9.5 6.5 4h11L21 9.5z"/><path d="M3 9.5 12 21l9-11.5M6.5 4 9 9.5 12 4l3 5.5L17.5 4M9 9.5 12 21l3-11.5"/></svg>""",
        ["partidas"] = IconSvg + """<path class="f" d="M7.5 7h9a5 5 0 0 1 4.7 6.7l-.9 2.6a2.4 2.4 0 0 1-4 .9L14.5 15h-5l-1.8 2.2a2.4 2.4 0 0 1-4-.9l-.9-2.6A5 5 0 0 1 7.5 7z"/><path d="M8 10v3.4M6.3 11.7h3.4"/><circle cx="15.5" cy="10.6" r=".5" fill="currentColor"/><circle cx="17.3" cy="12.6" r=".5" fill="currentColor"/></svg>""",
        ["companeros"] = IconSvg + """<circle class="f" cx="9" cy="8" r="3.5"/><path class="f" d="M2.5 20a6.5 6.5 0 0 1 13 0z"/><path d="M15.5 4.7a3.5 3.5 0 0 1 0 6.6M18 14a6.5 6.5 0 0 1 3.5 6"/></svg>""",
        ["ajustes"] = IconSvg + """<path d="M4 6.5h9M19 6.5h1M4 12h3M13 12h7M4 17.5h11M21 17.5h-1"/><circle class="f" cx="16" cy="6.5" r="2.5"/><circle class="f" cx="10" cy="12" r="2.5"/><circle class="f" cx="17.5" cy="17.5" r="2.5"/></svg>""",
    };

    // Iconos de las cifras del resumen.
    private const string KpiSvg = """<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private static readonly Dictionary<string, string> KpiIcons = new()
    {
        ["wins"] = KpiSvg + """<path class="f" d="M7 4h10v5a5 5 0 0 1-10 0z"/><path d="M8.5 21h7M12 14v7M17 5.5h3V7a3.5 3.5 0 0 1-3.3 3.5M7 5.5H4V7a3.5 3.5 0 0 0 3.3 3.5"/></svg>""",
        ["matches"] = KpiSvg + """<path class="f" d="M7.5 7h9a5 5 0 0 1 4.7 6.7l-.9 2.6a2.4 2.4 0 0 1-4 .9L14.5 15h-5l-1.8 2.2a2.4 2.4 0 0 1-4-.9l-.9-2.6A5 5 0 0 1 7.5 7z"/><path d="M8 10v3.4M6.3 11.7h3.4"/></svg>""",
        ["avg"] = KpiSvg + """<rect class="f" x="4" y="12" width="4" height="8" rx="1"/><rect class="f" x="10" y="5" width="4" height="15" rx="1"/><rect class="f" x="16" y="9" width="4" height="11" rx="1"/></svg>""",
        ["best"] = KpiSvg + """<path class="f" d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/></svg>""",
        ["day"] = KpiSvg + """<rect x="3.5" y="5" width="17" height="15.5" rx="2.5"/><path class="f" d="M3.5 7.5A2.5 2.5 0 0 1 6 5h12a2.5 2.5 0 0 1 2.5 2.5V10h-17z"/><path d="M8 3v4M16 3v4"/><rect x="7" y="13" width="3" height="3" rx=".6" fill="currentColor" stroke="none"/></svg>""",
    };

    // Iconos de cada objeto de la caza del tesoro (por tipo; los artilugios, cada uno el suyo).
    private const string ItemSvg = """<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private static readonly Dictionary<Prizes.Kind, string> KindIcons = new()
    {
        [Prizes.Kind.Outfit] = ItemSvg + """<path class="f" d="M8.5 4 4 6.6l1.6 3.8 1.9-.8V20h9V9.6l1.9.8L20 6.6 15.5 4a3.5 3.5 0 0 1-7 0z"/></svg>""",
        [Prizes.Kind.Pickaxe] = ItemSvg + """<path class="f" d="M3.5 9C7 4.6 17 4.6 20.5 9 16.6 7.6 7.4 7.6 3.5 9z"/><path d="M12 7.2V21"/></svg>""",
        [Prizes.Kind.BackBling] = ItemSvg + """<rect class="f" x="5" y="7" width="14" height="13.5" rx="3.5"/><path d="M9 7V5.5a3 3 0 0 1 6 0V7M8.5 13.5h7v3.5h-7z"/></svg>""",
        [Prizes.Kind.Vehicle] = ItemSvg + """<path class="f" d="M3 16v-3l2.2-4.5a2 2 0 0 1 1.8-1.1h8.4a2 2 0 0 1 1.5.7L20 12l1 .5V16z"/><circle cx="7.5" cy="16.5" r="2" fill="#121215"/><circle cx="16.5" cy="16.5" r="2" fill="#121215"/><path d="M6 12h13"/></svg>""",
        [Prizes.Kind.Decal] = ItemSvg + """<path class="f" d="M5 4h14v10l-6 6H5z"/><path d="M13 20v-4a2 2 0 0 1 2-2h4"/></svg>""",
        [Prizes.Kind.JamTrack] = ItemSvg + """<path d="M9 17.5V5.5l11-2v12"/><circle class="f" cx="6.5" cy="17.5" r="2.5"/><circle class="f" cx="17.5" cy="15.5" r="2.5"/></svg>""",
        [Prizes.Kind.LoadingScreen] = ItemSvg + """<rect class="f" x="3" y="5" width="18" height="14" rx="2.5"/><path d="m3.5 16.5 5-5 4 4 3-3 5 5"/><circle cx="16" cy="9.5" r="1.4"/></svg>""",
        [Prizes.Kind.ShopAccess] = ItemSvg + """<path class="f" d="M4.5 8h15l-1.2 12.5H5.7z"/><path d="M9 10V6.5a3 3 0 0 1 6 0V10"/></svg>""",
    };
    private static readonly Dictionary<string, string> GizmoIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Golden Firework"] = ItemSvg + """<path d="M12 3v3.5M12 17.5V21M3 12h3.5M17.5 12H21M5.6 5.6l2.5 2.5M15.9 15.9l2.5 2.5M5.6 18.4l2.5-2.5M15.9 8.1l2.5-2.5"/><circle class="f" cx="12" cy="12" r="2.8"/></svg>""",
        ["Portable Extractor"] = ItemSvg + """<rect class="f" x="7" y="7" width="10" height="13.5" rx="2.5"/><path d="M10 7V3.5h4V7M7 12h10M12 15v2"/></svg>""",
        ["Sprite Locator"] = ItemSvg + """<circle cx="12" cy="12" r="8.5"/><path class="f" d="m15.6 8.4-2.1 5.1-5.1 2.1 2.1-5.1z"/></svg>""",
        ["Lucky Locator"] = ItemSvg + """<circle class="f" cx="9" cy="9" r="3"/><circle class="f" cx="15" cy="9" r="3"/><circle class="f" cx="9" cy="15" r="3"/><circle class="f" cx="15" cy="15" r="3"/><path d="m15.5 15.5 4 5"/></svg>""",
        ["Extraction Accelerator"] = ItemSvg + """<path class="f" d="M13.5 2.5 4.5 14h7l-1 7.5 9-11.5h-7z"/></svg>""",
    };
    private const string GemIconBig = ItemSvg + """<path class="f" d="M3 9.5 6.5 4h11L21 9.5z"/><path d="M3 9.5 12 21l9-11.5M9 9.5 12 21l3-11.5"/></svg>""";

    /// <summary>Icono de un tesoro por su nombre: el del artilugio o el de su tipo; si no se sabe qué es, la gema.</summary>
    private static string ItemIcon(string name) =>
        Prizes.ByName(name) is { } p
            ? p.Type == Prizes.Kind.Gizmo ? GizmoIcons.GetValueOrDefault(p.Name, GemIconBig) : KindIcons.GetValueOrDefault(p.Type, GemIconBig)
            : GemIconBig;

    private const string GemIcon = """<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h12l3 6-9 12L3 9z"/><path d="M3 9h18"/></svg>""";

    /// <summary>Título de cada sección, con su icono y una línea que dice qué hay en ella.</summary>
    private static string PageHeader(string id, string title, string subtitle) =>
        $"""<header class="ph"><span class="phi">{TabIcons.GetValueOrDefault(id)}</span><div><h1>{title}</h1><p>{subtitle}</p></div></header>""";

    /// <summary>
    /// Gráfico de línea de los cofres de cada día: la curva (SVG) se estira a todo el ancho; los números, las fechas,
    /// la media y los puntos van encima en HTML, así no se deforman a ningún tamaño.
    /// </summary>
    private static string ChestChart(List<DateTime> days, List<int> perDay)
    {
        const double w = 1000, h = 300;
        var max = NiceMax(perDay.Max());
        double X(int i) => i * w / (days.Count - 1);
        double Y(double v) => (1 - v / max) * h;
        string F(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);
        string Px(double x) => F(100 * x / w) + "%";
        string Py(double y) => F(100 * y / h) + "%";
        var sb = new StringBuilder($"""<div class="evo" role="img" aria-label="{H(L.T("Cofres por día, últimos 30 días", "Chests per day, last 30 days"))}">""");
        foreach (var v in new[] { 0, max / 2, max })
        {
            sb.Append($"""<span class="gl" style="top:{Py(Y(v))}"></span><span class="ya" style="top:{Py(Y(v))}">{v.ToString("#,0", Loc)}</span>""");
        }
        var played = perDay.Where(v => v > 0).ToList();
        if (played.Count > 0)
        {
            var avg = played.Average();
            sb.Append($"""<span class="al" style="top:{Py(Y(avg))}"><i>{H(L.T("media por día jugado", "avg per day played"))} {avg.ToString("0", Loc)}</i></span>""");
        }
        // Curva suave (Catmull-Rom → Bézier) sin bajar de cero.
        var pts = perDay.Select((v, i) => (X: X(i), Y: Y(v))).ToList();
        var path = new StringBuilder($"M{F(pts[0].X)},{F(pts[0].Y)}");
        for (var i = 0; i < pts.Count - 1; i++)
        {
            var p0 = pts[Math.Max(0, i - 1)]; var p1 = pts[i]; var p2 = pts[i + 1]; var p3 = pts[Math.Min(pts.Count - 1, i + 2)];
            double C(double y) => Math.Min(h, Math.Max(0, y));
            path.Append($" C{F(p1.X + (p2.X - p0.X) / 6)},{F(C(p1.Y + (p2.Y - p0.Y) / 6))} {F(p2.X - (p3.X - p1.X) / 6)},{F(C(p2.Y - (p3.Y - p1.Y) / 6))} {F(p2.X)},{F(p2.Y)}");
        }
        sb.Append($"""<svg viewBox="0 0 {F(w)} {F(h)}" preserveAspectRatio="none" aria-hidden="true"><defs><linearGradient id="evoFill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#e8b04b" stop-opacity=".32"/><stop offset="1" stop-color="#e8b04b" stop-opacity="0"/></linearGradient></defs>""");
        sb.Append($"""<path class="area" d="{path} L{F(w)},{F(h)} L0,{F(h)} Z"/><path class="line" d="{path}"/></svg>""");
        for (var i = 0; i < pts.Count; i++)
        {
            var tip = $"{days[i].ToString(L.En ? "ddd MMM d" : "ddd d MMM", Loc)} · {L.Count(perDay[i], "cofre", "cofres", "chest", "chests")}";
            if (perDay[i] > 0)
            {
                sb.Append($"""<span class="pt{(i == pts.Count - 1 ? " now" : "")}" style="left:{Px(pts[i].X)};top:{Py(pts[i].Y)}" title="{H(tip)}"></span>""");
            }
            if (i == pts.Count - 1 || (pts.Count - 1 - i) % 5 == 0)
            {
                var label = i == pts.Count - 1 ? L.T("hoy", "today") : days[i].ToString(L.En ? "MMM d" : "d MMM", Loc);
                sb.Append($"""<span class="xa{(i == pts.Count - 1 ? " now" : "")}" style="left:{Px(pts[i].X)}">{H(label)}</span>""");
            }
        }
        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>Últimas partidas del resumen: lo esencial en una tabla corta.</summary>
    private static void AppendCompactMatches(StringBuilder sb, List<MatchRecord> rows)
    {
        sb.Append($"""<div class="card tw"><table class="compact"><thead><tr><th>{L.T("Fecha", "Date")}</th><th>{L.T("Modo", "Mode")}</th><th class="c">{L.T("Puesto", "Placement")}</th><th class="c">{L.T("Elim.", "Elims")}</th><th class="c">{L.T("Cofres", "Chests")}</th><th class="c">{L.T("Tesoros", "Treasures")}</th></tr></thead><tbody>""");
        if (rows.Count == 0)
        {
            sb.Append($"<tr><td colspan=\"6\" class=\"empty\">{L.T("Aún no hay partidas analizadas. Juega una y aparecerá aquí.", "No matches analyzed yet. Play one and it will show up here.")}</td></tr>");
        }
        foreach (var m in rows)
        {
            var placeClass = m.Placement switch { 1 => " win", > 0 and <= 10 => " top", _ => "" };
            var place = m.Placement == 1 ? "🏆 " + H(Pretty.Placement(m)) : H(Pretty.Placement(m));
            var treasures = m.TreasureFinds.Count > 0 ? $"<span class=\"gemc\">{m.TreasureFinds.Count}{GemIcon}</span>" : "<span class=\"muted\">—</span>";
            sb.Append($"<tr{(m.Placement == 1 ? " class=\"won\"" : "")}><td class=\"nw\">{L.DateTimeShort(When(m))}</td><td class=\"sm\">{H(Pretty.Playlist(m.Playlist))}</td><td class=\"c\"><span class=\"badge{placeClass}\">{place}</span></td>"
                + $"<td class=\"c\">{(m.Eliminations is int e ? N0(e) : "<span class=\"muted\">—</span>")}</td><td class=\"c\"><b>{N0(m.ChestsOpened)}</b></td><td class=\"c\">{treasures}</td></tr>");
        }
        sb.Append("</tbody></table></div>");
    }

    /// <summary>Últimos tesoros conseguidos (resumen): icono del objeto, nombre, partida y tipo.</summary>
    private static string LatestTreasures(List<(MatchRecord m, TreasureFind f)> rows, Func<int, string> N)
    {
        var sb = new StringBuilder($"""<section class="grow"><div class="sh"><h2>{L.T("Últimos tesoros", "Latest treasures")}</h2>{(rows.Count > 0 ? $"<a class=\"more\" href=\"#tesoros\" data-go=\"tesoros\">{L.F("Ver los {0}", "See all {0}", N(rows.Count))} →</a>" : "")}</div><div class="card side-card">""");
        if (rows.Count == 0)
        {
            sb.Append($"<p class=\"sm empty-side\">{L.T("Aún no has encontrado tesoros.", "No treasures yet.")}</p></div></section>");
            return sb.ToString();
        }
        sb.Append("<ul class=\"tl\">");
        foreach (var (m, f) in rows.OrderByDescending(r => When(r.m)).ThenByDescending(r => r.f.ObtainedAtSeconds ?? -1).Take(8))
        {
            var (name, type, featured) = Prizes.Describe(f, When(m));
            var kind = featured ? (type != null ? $"{Prizes.Featured} · {type}" : Prizes.Featured) : type ?? L.T("Artilugio", "Gizmo");
            sb.Append($"<li><span class=\"gem{(featured ? " f" : "")}\">{ItemIcon(name)}</span><span class=\"tn\"><b>{H(name)}</b><small>{L.DateTimeShort(When(m))} · {H(kind)}</small></span></li>");
        }
        sb.Append("</ul></div></section>");
        return sb.ToString();
    }

    /// <summary>Compañeros en el resumen, siempre a la vista: cuánto aportan al equipo y con quién juegas más.</summary>
    private static string TeamSummary(List<MatchRecord> matches, IReadOnlyCollection<string> kinds, Func<int, string> N)
    {
        var team = matches.Where(m => m.Teammates is { Count: > 0 }).ToList();
        if (team.Count == 0)
        {
            return "";
        }
        var mine = team.Sum(m => m.OwnChests);
        var theirs = team.Sum(m => m.TeammatesSeen(kinds));
        var share = mine + theirs > 0 ? 100.0 * theirs / (mine + theirs) : 0;
        var top = team.SelectMany(m => m.Teammates!).GroupBy(t => t.EpicId ?? t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Name: g.Last().Name, Matches: g.Count())).OrderByDescending(x => x.Matches).First();
        return $"""
            <section class="teamstrip"><a class="card team2" href="#companeros" data-go="companeros" aria-label="{L.T("Ver compañeros", "See teammates")}">
            <div><span class="sm">{L.T("Cofres del equipo abiertos por tus compañeros", "Team chests opened by your teammates")}</span><b>≥ {share.ToString("0", Loc)}&nbsp;%</b><span class="track"><i style="width:{share.ToString("0.#", CultureInfo.InvariantCulture)}%"></i></span></div>
            <div><span class="sm">{L.T("Con quién más juegas", "Who you play with most")}</span><b class="nm">{H(top.Name)}</b><span class="sm">{L.Count(top.Matches, "partida juntos", "partidas juntos", "match together", "matches together")}</span></div>
            <span class="go" aria-hidden="true">→</span></a></section>
            """;
    }

    // Iconos de la página de Configuración.
    private static readonly Dictionary<string, string> SetIcons = new()
    {
        ["lang"] = KpiSvg + """<circle class="f" cx="12" cy="12" r="8.5"/><path d="M3.5 12h17M12 3.5c2.6 2.4 3.6 5.4 3.6 8.5s-1 6.1-3.6 8.5M12 3.5C9.4 5.9 8.4 8.9 8.4 12s1 6.1 3.6 8.5"/></svg>""",
        ["mates"] = KpiSvg + """<circle class="f" cx="9" cy="8" r="3.5"/><path class="f" d="M2.5 20a6.5 6.5 0 0 1 13 0z"/><path d="M15.5 4.7a3.5 3.5 0 0 1 0 6.6M18 14a6.5 6.5 0 0 1 3.5 6"/></svg>""",
        ["sum"] = KpiSvg + """<circle class="f" cx="12" cy="12" r="8.5"/><path d="M12 8v8M8 12h8"/></svg>""",
        ["ammo"] = KpiSvg + """<path class="f" d="M5 10.5a2 2 0 0 1 4 0V20H5zM10 8.5a2 2 0 0 1 4 0V20h-4zM15 10.5a2 2 0 0 1 4 0V20h-4z"/><path d="M4 20h16"/></svg>""",
        ["food"] = KpiSvg + """<path class="f" d="M12 7.5c-2-1.6-7-1.2-7 4.8 0 4 2.9 8.2 5 8.2 1 0 1.4-.5 2-.5s1 .5 2 .5c2.1 0 5-4.2 5-8.2 0-6-5-6.4-7-4.8z"/><path d="M12 7.5c0-2 1-3.5 3-4.3"/></svg>""",
        ["cannons"] = KpiSvg + """<circle class="f" cx="8" cy="16" r="4"/><path d="m10.5 13 8-6.5 2.2 3-8.4 6"/><path d="M4 20h8"/></svg>""",
    };

    /// <summary>
    /// Pestaña «Configuración» de Estadísticas: el idioma es bidireccional (cambia la página y la app); compañeros y
    /// qué cuenta como cofre solo cambian lo que se ve en esta página. Se cambia aquí mismo cuando la página la sirve
    /// FCT (http://localhost); abierta como archivo, se ve pero no se cambia. Aquí, y solo aquí, se ve la versión.
    /// </summary>
    private static string SettingsPanel(Config config)
    {
        var r = config.ReportView;
        string Row(string icon, string label, string? hint, string control, string cls = "") =>
            $"""<div class="setrow{cls}"><span class="si">{SetIcons[icon]}</span><div class="st"><b>{label}</b>{(hint != null ? $"<small>{hint}</small>" : "")}</div>{control}</div>""";
        string Switch(string key, bool on, string label) =>
            $"""<label class="sw"><input type="checkbox" role="switch" data-key="{key}"{(on ? " checked" : "")} aria-label="{H(label)}"><span></span></label>""";
        string Toggle(string icon, string key, bool on, string label, string? hint = null) => Row(icon, label, hint, Switch(key, on, label));

        var chosenLang = config.Language != null && L.Languages.Any(l => l.Code == config.Language) ? config.Language : null;
        var sysLanguage = L.Languages.FirstOrDefault(l => l.Code == L.SystemCode).Name ?? "English";
        var langs = new StringBuilder();
        langs.Append($"""<option value=""{(chosenLang == null ? " selected" : "")}>{H(L.F("Sistema ({0})", "System ({0})", sysLanguage))}</option>""");
        foreach (var (code, name) in L.Languages)
        {
            langs.Append($"""<option value="{code}"{(chosenLang == code ? " selected" : "")}>{H(name)}</option>""");
        }
        var offline = ObsServer.ReportUrl is { } url
            ? L.T("Abre las estadísticas con el botón Estadísticas de FCT para cambiar la configuración aquí.", "Open stats with the FCT Stats button to change the settings here.") + $" <a href=\"{url}\">{L.T("Abrir", "Open")} →</a>"
            : L.T("Abre FCT para cambiar la configuración aquí.", "Open FCT to change the settings here.");
        var logo = AppLogo.DataUri is { } l ? $"<img src=\"{l}\" alt=\"\" width=\"76\" height=\"76\">" : "";
        return $"""
            {PageHeader("ajustes", L.T("Configuración", "Settings"), L.T("El idioma se comparte con la app; el resto solo cambia estas estadísticas", "Language is shared with the app; the rest only changes these stats"))}
            <p class="offline-note" hidden>{offline}</p>
            <div class="card abouthero">
              {logo}<div class="ai"><b>{AppInfo.Name}</b><span class="ver">{L.F("Versión {0}", "Version {0}", AppInfo.VersionText)}</span><small>{L.T("Cuenta tus cofres de Fortnite a partir de tus replays.", "Counts your Fortnite chests from your replays.")}</small></div>
              <div class="links"><a href="{AppInfo.RepoUrl}" target="_blank" rel="noopener">GitHub →</a><a href="{AppInfo.RepoUrl}/releases" target="_blank" rel="noopener">{L.T("Novedades", "What's new")} →</a><a href="{AppInfo.IssuesUrl}" target="_blank" rel="noopener">{L.T("Informar de un problema", "Report a problem")} →</a></div>
            </div>
            <div class="setgrid">
              <section><div class="sh"><h2>{L.T("Idioma", "Language")}</h2></div><div class="card set">
                {Row("lang", L.T("Idioma de la interfaz", "Interface language"), L.T("Se aplica también en la app (se reinicia sola)", "Also applied to the app (restarts automatically)"), $"<select class=\"sel\" data-key=\"lang\" aria-label=\"{H(L.T("Idioma de la interfaz", "Interface language"))}\">{langs}</select>", " col")}
              </div></section>
              <section><div class="sh"><h2>{L.T("Compañeros", "Teammates")}</h2></div><div class="card set">
                {Toggle("mates", "teammates", r.ShowTeammates, L.T("Mostrar compañeros", "Show teammates"), L.T("Con quién juegas y sus cofres (hasta 1.087 m de ti)", "Who you play with and their chests (up to 1,087 m from you)"))}
                {Toggle("sum", "countMates", r.CountTeammateChests, L.T("Sumar sus cofres a tu total", "Add their chests to your total"), L.T("Tu historial no cambia", "Your history does not change"))}
              </div></section>
              <section><div class="sh"><h2>{L.T("Contar también como cofre", "Also count as a chest")}</h2></div><div class="card set">
                {Toggle("ammo", "ammo", r.CountAmmoBoxes, L.T("Cajas de munición", "Ammo boxes"))}
                {Toggle("food", "food", r.CountFoodBoxes, L.T("Cajas de comida", "Food boxes"))}
                {Toggle("cannons", "cannons", r.CountLootCannons, L.T("Cañones de botín", "Loot cannons"))}
              </div></section>
            </div>
            <div class="toast" role="status" aria-live="polite" hidden></div>
            """;
    }

    public static string Report(Config config, TrackerData data, IReadOnlyList<Profile> profiles, int initialChests = 0)
    {
        var progress = Progress;
        var matches = data.Matches.OrderByDescending(When).ToList();
        var countedChests = matches.Sum(m => m.ChestsOpened);
        var totalChests = countedChests + Math.Max(0, initialChests);
        var avg = matches.Count > 0 ? (double)countedChests / matches.Count : 0;
        var best = matches.Count > 0 ? matches.Max(m => m.ChestsOpened) : 0;
        var treasures = matches.Sum(m => m.TreasureFinds.Count);
        var todayMatches = matches.Where(m => When(m).Date == DateTime.Now.Date).ToList();
        var todayChests = todayMatches.Sum(m => m.ChestsOpened);
        var wins = matches.Count(m => m.Placement == 1);
        var player = matches.Select(m => m.PlayerName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? "";
        string N(int n) => n.ToString("#,0", Loc);

        var sb = new StringBuilder();
        var title = AppInfo.Name;
        var logo = AppLogo.DataUri;
        sb.Append($"""
            <!doctype html><html lang="{L.Code}"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="color-scheme" content="dark">
            <noscript><meta http-equiv="refresh" content="{(progress != null ? 2 : 15)}"></noscript>
            <title>{title}{(player.Length > 0 ? " · " + H(player) : "")}</title>{(logo != null ? $"<link rel=\"icon\" href=\"{logo}\">" : "")}<style>{Style}</style></head>
            <body data-refresh="{(progress != null ? 2 : 15)}" data-saved="{H(L.T("Guardado", "Saved"))}" data-failed="{H(L.T("No se pudo guardar", "Could not save"))}" data-offline="{H(L.F("{0} está cerrado: las cifras no se actualizan", "{0} is closed: the figures are not updating", AppInfo.ShortName))}"><div class="app">
            """);

        // ---- Datos que comparten varias pestañas -----------------------------------------------------
        var extraKinds = config.ExtraKinds();
        var treasureRows = matches.SelectMany(m => m.TreasureFinds.Select(f => (m, f))).ToList();
        (string Html, int Count)? mates = config.ShowTeammates ? TeammatesPanel(matches, extraKinds, N) : null;
        var bestDay = matches.GroupBy(m => When(m).Date).Select(g => (Day: g.Key, Chests: g.Sum(m => m.ChestsOpened)))
            .OrderByDescending(d => d.Chests).FirstOrDefault();

        // ---- Barra lateral: logo y secciones (arriba en el móvil) -------------------------------------
        var tabs = new List<(string Id, string Label, string? Count)>
        {
            ("resumen", L.T("Resumen", "Overview"), null),
            ("cofres", L.T("Cofres", "Chests"), null),
            ("tesoros", L.T("Tesoros", "Treasures"), treasureRows.Count > 0 ? N(treasureRows.Count) : null),
            ("partidas", L.T("Partidas", "Matches"), N(matches.Count)),
        };
        if (mates != null)
        {
            tabs.Add(("companeros", L.T("Compañeros", "Teammates"), mates.Value.Count > 0 ? N(mates.Value.Count) : null));
        }
        sb.Append($"""<aside class="side"><div class="brand">{(logo != null ? $"<img src=\"{logo}\" alt=\"\" width=\"52\" height=\"52\">" : $"<span class=\"bl\">{ChestIcon}</span>")}<div class="bn"><span>Fortnite</span><b>Chest Tracker</b></div></div>""");
        sb.Append($"""<nav class="nav" role="tablist" aria-label="{L.T("Secciones de estadísticas", "Stats sections")}" aria-orientation="vertical">""");
        foreach (var (id, label, count) in tabs)
        {
            var countText = count != null ? $"<span class=\"c\" aria-label=\"{H(L.F("{0} en total", "{0} in total", count))}\">{count}</span>" : "";
            sb.Append($"""<button class="tab" role="tab" type="button" id="tab-{id}" data-tab="{id}" aria-controls="t-{id}">{TabIcons.GetValueOrDefault(id)}<span>{label}</span>{countText}</button>""");
        }
        sb.Append($"""<button class="tab end" role="tab" type="button" id="tab-ajustes" data-tab="ajustes" aria-controls="t-ajustes">{TabIcons["ajustes"]}<span>{L.T("Configuración", "Settings")}</span></button>""");
        sb.Append("</nav></aside><main class=\"main\"><div class=\"offline\" role=\"status\" hidden></div>");
        sb.Append(LoadingCard(progress));

        // ================================ Resumen ================================
        sb.Append("""<div class="panel" id="t-resumen" role="tabpanel" aria-labelledby="tab-resumen">""");
        var analyzed = L.Count(matches.Count, "partida analizada", "partidas analizadas", "match analyzed", "matches analyzed");
        sb.Append(PageHeader("resumen", L.T("Resumen", "Overview"), player.Length > 0 ? $"{H(player)} · {analyzed}" : analyzed));
        var notes = new List<string>();
        if (initialChests > 0)
        {
            notes.Add(L.F("Incluye {0} estimados de antes de usar la herramienta", "Includes {0} estimated from before the tool", N(initialChests)));
        }
        var matesAdded = matches.Sum(m => m.TeammatesAdded);
        if (config.CountTeammateChests && matesAdded > 0)
        {
            notes.Add(L.F("incluye ≥ {0} de tus compañeros", "includes ≥ {0} from your teammates", N(matesAdded)));
        }
        var winRate = matches.Count > 0 ? 100.0 * wins / matches.Count : 0;
        var todayLine = $"{L.T("Hoy", "Today")}: <b>{N(todayChests)}</b>" + (todayMatches.Count > 0 ? " · " + L.Count(todayMatches.Count, "partida", "partidas", "match", "matches") : "");
        string Kpi(string icon, string label, string value, string sub, string cls = "") =>
            $"""<div class="kpi"><span class="ki">{icon}</span><div class="l">{label}</div><div class="v{cls}">{value}</div><div class="s">{sub}</div></div>""";
        sb.Append($"""
            <div class="kpis">
              <div class="kpi hero">{(logo != null ? $"<img src=\"{logo}\" alt=\"\" width=\"88\" height=\"88\">" : "")}<div>
                <div class="l">{L.T("Cofres abiertos", "Chests opened")}</div><div class="v">{N(totalChests)}</div>
                <div class="s today">{todayLine}</div>{(notes.Count > 0 ? $"<div class=\"s\">{string.Join(" · ", notes)}</div>" : "")}</div></div>
              {Kpi(KpiIcons["wins"], L.T("Victorias", "Wins"), N(wins), wins > 0 ? L.F("{0} % de tus partidas", "{0} % of your matches", winRate.ToString("0", Loc)) : "—")}
              {Kpi(KpiIcons["matches"], L.T("Partidas", "Matches"), N(matches.Count), L.T("analizadas", "analyzed"))}
              {Kpi(KpiIcons["avg"], L.T("Media", "Average"), avg.ToString("0.0", Loc), L.T("cofres por partida", "chests per match"))}
              {Kpi(KpiIcons["best"], L.T("Récord", "Best match"), N(best), L.T("cofres en una partida", "chests in one match"))}
              {Kpi(KpiIcons["day"], L.T("Mejor día", "Best day"), bestDay.Chests > 0 ? bestDay.Day.ToString(L.En ? "MMM d" : "d MMM", Loc) : "—", bestDay.Chests > 0 ? L.Count(bestDay.Chests, "cofre", "cofres", "chest", "chests") : "—")}
            </div>
            """);

        // Evolución de los últimos 30 días.
        var today = DateTime.Now.Date;
        var days = Enumerable.Range(0, 30).Select(i => today.AddDays(i - 29)).ToList();
        var perDay = days.Select(d => matches.Where(m => When(m).Date == d).Sum(m => m.ChestsOpened)).ToList();
        var sum30 = perDay.Sum();
        var activeDays = perDay.Count(v => v > 0);
        sb.Append($"""
            <section class="chartsec"><div class="sh"><h2>{L.T("Evolución de cofres", "Chests over time")}</h2>
            <span class="meta">{L.T("Últimos 30 días", "Last 30 days")} · {L.Count(sum30, "cofre", "cofres", "chest", "chests")} · {L.Count(activeDays, "día jugado", "días jugados", "day played", "days played")}</span></div>
            <div class="card pad">{ChestChart(days, perDay)}</div></section>
            """);

        // Abajo: últimas partidas a la izquierda; últimos tesoros y compañeros a la derecha.
        sb.Append("<div class=\"duo\"><section class=\"fill\">");
        sb.Append($"""<div class="sh"><h2>{L.T("Últimas partidas", "Latest matches")}</h2>{(matches.Count > 6 ? $"<a class=\"more\" href=\"#partidas\" data-go=\"partidas\">{L.T("Ver todas", "See all")} →</a>" : "")}</div>""");
        AppendCompactMatches(sb, matches.Take(6).ToList());
        sb.Append("</section><div class=\"stack\">");
        sb.Append(LatestTreasures(treasureRows, N));
        if (mates != null)
        {
            sb.Append(TeamSummary(matches, extraKinds, N));
        }
        sb.Append("</div></div></div>");

        // ================================ Cofres ================================
        sb.Append("""<div class="panel" id="t-cofres" role="tabpanel" aria-labelledby="tab-cofres">""");
        sb.Append(PageHeader("cofres", L.T("Cofres", "Chests"), L.T("Por tipo de cofre y modo de juego", "By chest type and game mode")));
        var byKind = matches.SelectMany(m => m.Chests).GroupBy(c => c.Kind).Select(g => (Kind: g.Key, Count: g.Count())).OrderByDescending(g => g.Count).ToList();
        var otherKinds = matches
            .SelectMany(m => m.OtherInteractions.Count > 0
                ? m.OtherInteractions
                : new Dictionary<string, int> { [ChestKinds.AmmoBox] = m.AmmoBoxesOpened, [ChestKinds.OtherContainer] = m.OtherContainersOpened })
            .GroupBy(kv => kv.Key)
            .Select(g => (Kind: g.Key, Count: g.Sum(kv => kv.Value)))
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .ToList();
        var kindTotal = Math.Max(1, byKind.Sum(k => k.Count));
        var kindMax = Math.Max(1, byKind.Select(k => k.Count).DefaultIfEmpty(0).Max());

        sb.Append("<div class=\"grid2\">");
        sb.Append($"<section><div class=\"sh\"><h2>{L.T("Por tipo de cofre", "By chest type")}</h2><span class=\"meta\">{L.Count(byKind.Sum(k => k.Count), "cofre", "cofres", "chest", "chests")}</span></div><div class=\"card pad\"><div class=\"rows\">");
        if (byKind.Count == 0)
        {
            sb.Append($"<div class=\"empty\">{L.T("Aún no hay cofres registrados.", "No chests recorded yet.")}</div>");
        }
        foreach (var (kind, count) in byKind)
        {
            var share = 100.0 * count / kindTotal;
            sb.Append($"<div class=\"row\"><span>{H(L.Name(kind))}</span><span class=\"n\">{N(count)}<small>{share.ToString("0", Loc)} %</small></span><div class=\"track\"><i style=\"width:{(100.0 * count / kindMax).ToString("0.##", CultureInfo.InvariantCulture)}%\"></i></div></div>");
        }
        var destroyedTotal = matches.Sum(m => m.Chests.Count(c => c.Destroyed));
        if (destroyedTotal > 0)
        {
            sb.Append($"<div class=\"row sub\"><span>{L.T("de ellos, destruidos en vez de abiertos", "of which destroyed instead of opened")}</span><span class=\"n\">{N(destroyedTotal)}</span></div>");
        }
        sb.Append("</div></div></section>");

        // Cajas de munición, de comida y cañones: aparte, con una etiqueta que dice si suman al total.
        sb.Append($"<section><div class=\"sh\"><h2>{L.T("Otros contenedores", "Other containers")}</h2><span class=\"meta\">{L.T("se activan en Configuración", "turned on in Settings")}</span></div><div class=\"card pad\"><div class=\"rows\">");
        if (otherKinds.Count == 0)
        {
            sb.Append($"<div class=\"empty\">{L.T("Aún no hay cajas ni cañones registrados.", "No boxes or cannons recorded yet.")}</div>");
        }
        var otherMax = Math.Max(1, otherKinds.Select(k => k.Count).DefaultIfEmpty(0).Max());
        foreach (var (kind, count) in otherKinds)
        {
            var added = extraKinds.Contains(kind);
            var pill = added ? $"<span class=\"badge top\">{L.T("suma al total", "added to the total")}</span>" : $"<span class=\"badge\">{L.T("fuera del total", "not in the total")}</span>";
            sb.Append($"<div class=\"row{(added ? "" : " off")}\"><span class=\"lbl\">{H(L.Name(kind))} {pill}</span><span class=\"n\">{N(count)}</span><div class=\"track\"><i style=\"width:{(100.0 * count / otherMax).ToString("0.##", CultureInfo.InvariantCulture)}%\"></i></div></div>");
        }
        sb.Append("</div></div></section></div>");

        // Por modo de juego: dónde abres más cofres.
        var byMode = matches.GroupBy(m => Pretty.Playlist(m.Playlist))
            .Select(g => (Mode: g.Key, Matches: g.Count(), Chests: g.Sum(m => m.ChestsOpened), Best: g.Max(m => m.ChestsOpened), Treasures: g.Sum(m => m.TreasureFinds.Count)))
            .OrderByDescending(x => x.Matches).ToList();
        if (byMode.Count > 0)
        {
            var modeMax = Math.Max(0.1, byMode.Max(x => (double)x.Chests / x.Matches));
            sb.Append($"""
                <section class="fill"><div class="sh"><h2>{L.T("Por modo de juego", "By game mode")}</h2><span class="meta">{L.Count(byMode.Count, "modo", "modos", "mode", "modes")}</span></div>
                <div class="card tw"><table><thead><tr><th>{L.T("Modo", "Mode")}</th><th class="c">{L.T("Partidas", "Matches")}</th><th class="c">{L.T("Media", "Average")}</th><th class="c">{L.T("Récord", "Best")}</th><th class="c">{L.T("Cofres", "Chests")}</th><th class="c">{L.T("Tesoros", "Treasures")}</th></tr></thead><tbody>
                """);
            foreach (var x in byMode)
            {
                var a = (double)x.Chests / x.Matches;
                var w = (100.0 * a / modeMax).ToString("0.##", CultureInfo.InvariantCulture);
                sb.Append($"<tr><td><span class=\"badge mode\">{H(x.Mode)}</span></td><td class=\"c\">{N(x.Matches)}</td><td><div class=\"chests\"><span class=\"mini\"><i style=\"width:{w}%\"></i></span><b>{a.ToString("0.0", Loc)}</b></div></td><td class=\"c\">{N(x.Best)}</td><td class=\"c\">{N(x.Chests)}</td><td class=\"c\">{(x.Treasures > 0 ? $"<span class=\"gemc\">{N(x.Treasures)}{GemIcon}</span>" : "<span class=\"muted\">—</span>")}</td></tr>");
            }
            sb.Append("</tbody></table></div></section>");
        }
        sb.Append("</div>");

        // ================================ Tesoros ================================
        sb.Append("""<div class="panel" id="t-tesoros" role="tabpanel" aria-labelledby="tab-tesoros">""");
        sb.Append(PageHeader("tesoros", L.T("Tesoros", "Treasures"), L.T("Los que hay ahora y los que has conseguido", "The ones available now and the ones you got")));
        if (treasureRows.Count > 0)
        {
            var unusual = treasureRows.Where(r => !r.f.SourceIsChest).ToList();
            if (unusual.Count > 0)
            {
                var kinds = H(string.Join(", ", unusual.Select(r => L.Name(r.f.SourceKind)).Distinct()));
                sb.Append($"<div class=\"flag\"><span class=\"ic\">⚑</span><div><b>{L.Count(unusual.Count, "tesoro obtenido", "tesoros obtenidos", "treasure obtained", "treasures obtained")} {L.T("sin abrir un cofre", "other than from a chest")}</b>: {kinds}. {L.T("Resaltados en la tabla de abajo.", "Highlighted in the table below.")}</div></div>");
            }

            var featured = treasureRows.Count(r => r.f.Featured);
            var treasureMatches = treasureRows.Select(r => r.m).Distinct().Count();
            sb.Append($"""
                <div class="minis">
                  <div class="mini-kpi"><b class="gold">{N(treasureRows.Count)}</b><span>{L.T("tesoros en total", "treasures in total")}</span></div>
                  <div class="mini-kpi"><b>{N(featured)}</b><span>{L.T("tesoros destacados", "featured treasures")}</span></div>
                  <div class="mini-kpi"><b>{N(treasureRows.Count - featured)}</b><span>{L.T("artilugios de tesoro", "treasure gizmos")}</span></div>
                  <div class="mini-kpi"><b>{(matches.Count > 0 ? (100.0 * treasureMatches / matches.Count).ToString("0", Loc) : "0")} %</b><span>{L.T("de partidas con tesoro", "of matches with a treasure")}</span></div>
                </div>
                """);

            sb.Append("<div class=\"trow\">");
            var weeks = AppendTreasureHunt(sb, treasureRows);

            var bySource = treasureRows.GroupBy(r => L.Name(r.f.SourceKind)).OrderByDescending(g => g.Count()).ToList();
            var srcMax = Math.Max(1, bySource.Max(g => g.Count()));
            sb.Append($"<section><div class=\"sh\"><h2>{L.T("Dónde consigo mis tesoros", "Where I get my treasures")}</h2><span class=\"meta\">{L.Count(treasureRows.Count, "tesoro", "tesoros", "treasure", "treasures")}</span></div><div class=\"card pad\"><div class=\"rows\">");
            foreach (var g in bySource)
            {
                var isChest = g.First().f.SourceIsChest;
                sb.Append($"<div class=\"row{(isChest ? "" : " warn")}\"><span>{(isChest ? "" : "⚑ ")}{H(g.Key)}</span><span class=\"n\">{N(g.Count())}</span><div class=\"track\"><i style=\"width:{(100.0 * g.Count() / srcMax).ToString("0.##", CultureInfo.InvariantCulture)}%\"></i></div></div>");
            }
            sb.Append("</div></div></section></div>");
            sb.Append(weeks);

            sb.Append($"""
                <section class="fill"><div class="sh"><h2>{L.T("Tesoros obtenidos", "Treasures obtained")}</h2><span class="meta">{L.Count(treasureRows.Count, "tesoro", "tesoros", "treasure", "treasures")}</span></div>
                <div class="card tw"><table><thead><tr><th>{L.T("Partida", "Match")}</th><th>{L.T("Tesoro", "Treasure")}</th><th>{L.T("Conseguido por", "Obtained by")}</th><th class="c">{L.T("Momento", "Time")}</th><th>{L.T("Origen", "Source")}</th></tr></thead><tbody>
                """);
            foreach (var (m, f) in treasureRows)
            {
                var when = f.ObtainedAtSeconds is float t ? (f.TimeExact ? Pretty.Clock(t) : $"<span class=\"sm\">{L.T("hacia", "around")}</span> {Pretty.Clock(t)}") : "<span class=\"muted\">—</span>";
                var source = $"{(f.SourceIsChest ? "" : "⚑ ")}{H(f.SourceLabel)}";
                // Quién abrió el cofre que dio el tesoro: tú, un compañero (por su nombre) o desconocido (lo recibió el
                // escuadrón, pero el cofre no está en el replay).
                var by = f.Finder == TreasureFind.FinderUnknown ? $"<span class=\"muted\">{H(f.FinderLabel(m))}</span>" : H(f.FinderLabel(m));
                sb.Append($"<tr{(f.SourceIsChest ? "" : " class=\"unusual\"")}><td class=\"nw\">{L.DateTimeShort(When(m))}<div class=\"sm\">{H(Pretty.Playlist(m.Playlist))}</div></td><td>{TreasureCell(f, When(m))}</td><td class=\"nw\">{by}</td><td class=\"c nw num\">{when}</td><td>{source}</td></tr>");
            }
            sb.Append("</tbody></table></div></section>");
        }
        else
        {
            sb.Append($"<div class=\"card\"><div class=\"empty big\"><div class=\"star\">★</div>{L.T("Aún no has encontrado tesoros. Aparecerán aquí con el momento y el cofre de origen.", "No treasures yet. They will show up here with the time and the chest they came from.")}</div></div>");
            sb.Append(AppendTreasureHunt(sb, treasureRows));
        }
        sb.Append("</div>");

        // ================================ Partidas ================================
        var shown = matches.Take(100).ToList();
        var withMates = mates != null;
        var shownMax = Math.Max(1, shown.Select(m => m.ChestsOpened).DefaultIfEmpty(0).Max());
        sb.Append($"""
            <div class="panel" id="t-partidas" role="tabpanel" aria-labelledby="tab-partidas">
            {PageHeader("partidas", L.T("Partidas", "Matches"), matches.Count > shown.Count ? L.F("las {0} más recientes de {1}", "latest {0} of {1}", shown.Count, N(matches.Count)) : L.Count(matches.Count, "partida", "partidas", "match", "matches"))}
            <section class="fill">
            """);
        AppendMatchTable(sb, shown, shownMax, withMates, extraKinds);
        sb.Append("</section></div>");

        // ================================ Compañeros ================================
        if (mates != null)
        {
            var teamCount = matches.Count(m => m.Teammates is { Count: > 0 });
            sb.Append($"""<div class="panel" id="t-companeros" role="tabpanel" aria-labelledby="tab-companeros">{PageHeader("companeros", L.T("Compañeros", "Teammates"), L.Count(teamCount, "partida en equipo", "partidas en equipo", "team match", "team matches"))}{mates.Value.Html}</div>""");
        }

        // ================================ Ajustes ================================
        sb.Append($"""<div class="panel" id="t-ajustes" role="tabpanel" aria-labelledby="tab-ajustes">{SettingsPanel(config)}</div>""");

        sb.Append($"</main></div><script>{TabsScript}</script></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Tabla de partidas (fecha, modo, puesto, cofres, tesoros). Con «Compañeros»: tus cofres, el equipo (≥) y, bajo
    /// el modo, los cofres abiertos de cada compañero.
    /// </summary>
    private static void AppendMatchTable(StringBuilder sb, List<MatchRecord> rows, int chestMax, bool withMates, IReadOnlyCollection<string> kinds)
    {
        var hasElims = rows.Any(m => m.Eliminations is > 0);
        var cols = 5 + (hasElims ? 1 : 0) + (withMates ? 1 : 0);
        var chestsHead = withMates ? L.T("Tú", "You") : L.T("Cofres", "Chests");
        sb.Append($"""<div class="card tw"><table class="matches"><thead><tr><th>{L.T("Fecha", "Date")}</th><th>{L.T("Modo", "Mode")}</th><th class="c">{L.T("Puesto", "Placement")}</th>{(hasElims ? $"<th class=\"c\">{L.T("Elim.", "Elims")}</th>" : "")}<th class="c">{chestsHead}</th>{(withMates ? $"<th class=\"c\">{L.T("Equipo", "Team")}</th>" : "")}<th>{L.T("Tesoros", "Treasures")}</th></tr></thead><tbody>""");
        if (rows.Count == 0)
        {
            sb.Append($"<tr><td colspan=\"{cols}\" class=\"empty\">{L.T("Aún no hay partidas analizadas. Juega una y aparecerá aquí.", "No matches analyzed yet. Play one and it will show up here.")}</td></tr>");
        }
        foreach (var m in rows)
        {
            var treasure = m.TreasureFinds.Count == 0 ? "<span class=\"muted\">—</span>"
                : "<div class=\"tlist\">" + string.Concat(m.TreasureFinds.Select(f => $"<span class=\"star\">{(f.Featured ? "★★" : "★")} {H(Prizes.Describe(f, When(m)).Name)}</span>")) + "</div>";
            var placeClass = m.Placement switch { 1 => " win", > 0 and <= 10 => " top", _ => "" };
            var placeText = m.Placement == 1 ? "🏆 " + H(Pretty.Placement(m)) : H(Pretty.Placement(m));
            var own = withMates ? m.OwnChests : m.ChestsOpened;
            var barW = (100.0 * own / Math.Max(1, chestMax)).ToString("0.##", CultureInfo.InvariantCulture);
            var team = withMates && m.Teammates is { Count: > 0 } t
                ? "<div class=\"mates-line\">" + string.Concat(t.OrderByDescending(x => MatchRecord.Seen(x, kinds))
                    .Select(x => $"<span class=\"chip\"><span class=\"cn\">{H(x.Name)}</span> <b>≥ {MatchRecord.Seen(x, kinds)}</b></span>")) + "</div>"
                : "";
            var teamCell = !withMates ? ""
                : m.Teammates is { Count: > 0 }
                    ? $"<td class=\"c nw\"><span class=\"sm\">≥</span> <b class=\"cy\">{N0(m.OwnChests + m.TeammatesSeen(kinds))}</b></td>"
                    : "<td class=\"c\"><span class=\"muted\">—</span></td>";
            var elims = hasElims ? $"<td class=\"c\">{(m.Eliminations is int e ? N0(e) : "<span class=\"muted\">—</span>")}</td>" : "";
            sb.Append($"<tr{(m.Placement == 1 ? " class=\"won\"" : "")}><td class=\"nw\">{L.DateTimeShort(When(m))}</td><td><span class=\"badge mode\">{H(Pretty.Playlist(m.Playlist))}</span>{team}</td><td class=\"c\"><span class=\"badge{placeClass}\">{placeText}</span></td>{elims}<td><div class=\"chests\"><span class=\"mini\"><i style=\"width:{barW}%\"></i></span><b>{own}</b></div></td>{teamCell}<td>{treasure}</td></tr>");
        }
        sb.Append("</tbody></table></div>");
    }

    private static string N0(int n) => n.ToString("#,0", Loc);

    /// <summary>Celda de un tesoro: estrella, nombre limpio y su tipo (o "Tesoro destacado") debajo.</summary>
    private static string TreasureCell(TreasureFind f, DateTime matchLocal)
    {
        var (name, type, featured) = Prizes.Describe(f, matchLocal);
        var tags = new List<string>();
        if (featured && name != Prizes.Featured)
        {
            tags.Add($"<span class=\"tag gold\">{H(Prizes.Featured)}</span>");
        }
        if (type != null)
        {
            tags.Add($"<span class=\"tag\">{H(type)}</span>");
        }
        return $"<div class=\"tname\"><span class=\"star\">{(featured ? "★★" : "★")}</span><span>{H(name)}</span></div>"
               + (tags.Count > 0 ? $"<div class=\"tags\">{string.Concat(tags)}</div>" : "");
    }

    /// <summary>
    /// Tesoros disponibles actualmente (caza del tesoro): los premios de esta semana como fichas (los conseguidos, en
    /// oro) y lo que puede tocar cualquier semana. Devuelve las cinco semanas, que van a lo ancho debajo de la fila y se
    /// abren con «Ver todas las semanas» (siguen abiertas aunque la página se recargue).
    /// </summary>
    private static string AppendTreasureHunt(StringBuilder sb, List<(MatchRecord m, TreasureFind f)> rows)
    {
        var got = rows.Select(r => Prizes.Describe(r.f, When(r.m)).Name)
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var today = DateTime.Now.Date;
        var thisWeek = Prizes.WeekOf(today);
        DateTime WeekEnd(int week) => week < Prizes.WeekStarts.Length ? Prizes.WeekStarts[week] : Prizes.End;
        string Day(DateTime d) => d.ToString(L.En ? "MMM d" : "d MMM", Loc);
        string Range(int week) => $"{Day(Prizes.WeekStarts[week - 1])} – {Day(WeekEnd(week).AddDays(-1))}";
        string Unconfirmed(Prizes.Prize p) => p.Announced ? "" : $"<span class=\"tag\">{L.T("sin confirmar", "unconfirmed")}</span>";
        string Icon(Prizes.Prize p) => p.Type == Prizes.Kind.Gizmo ? GizmoIcons.GetValueOrDefault(p.Name, GemIconBig) : KindIcons.GetValueOrDefault(p.Type, GemIconBig);
        string Tile(Prizes.Prize p)
        {
            var n = got.GetValueOrDefault(Prizes.Shown(p));
            return $"<div class=\"tile{(n > 0 ? " got" : "")}\"><span class=\"tt\"><i>{Icon(p)}</i><span>{H(Prizes.TypeName(p.Type))}</span></span><b>{H(Prizes.Shown(p))}</b>"
                   + (n > 0 ? $"<span class=\"won\">★ ×{n}</span>" : Unconfirmed(p)) + "</div>";
        }
        string Line(Prizes.Prize p)
        {
            var n = got.GetValueOrDefault(Prizes.Shown(p));
            return $"<li class=\"{(n > 0 ? "got" : "")}\"><i>{Icon(p)}</i><span>{H(Prizes.Shown(p))}</span>{(n > 0 ? $"<b>×{n}</b>" : "")}</li>";
        }

        var meta = thisWeek > 0
            ? $"{L.F("Semana {0}", "Week {0}", thisWeek)} · {Range(thisWeek)} · {L.F("quedan {0}", "{0} left", L.Count((WeekEnd(thisWeek) - today).Days, "día", "días", "day", "days"))}"
            : L.T("Evento del 1 de noviembre al 5 de diciembre", "Event from November 1 to December 5");
        sb.Append($"""<section><div class="sh"><h2>{L.T("Tesoros disponibles actualmente", "Treasures available now")}</h2><span class="meta">{meta}</span></div><div class="card hunt">""");

        // Esta semana: fichas grandes.
        var week = Prizes.All.Where(p => thisWeek > 0 && p.Week == thisWeek).ToList();
        if (week.Count > 0)
        {
            sb.Append($"""<div class="tiles">{string.Concat(week.Select(Tile))}</div>""");
        }
        else if (thisWeek > 0)
        {
            sb.Append($"""<p class="sm hunt-note">{L.T("Premios aún no anunciados.", "Prizes not announced yet.")}</p>""");
        }

        // Cualquier semana: acceso a la tienda y los artilugios.
        var chips = Prizes.All.Where(p => p.Week == 0).Select(p =>
        {
            var n = got.GetValueOrDefault(Prizes.Shown(p));
            return $"<span class=\"pchip{(n > 0 ? " got" : "")}\"><i>{Icon(p)}</i>{H(Prizes.Shown(p))}{(n > 0 ? $" <b>×{n}</b>" : "")}</span>";
        });
        sb.Append($"""<div class="always"><span class="al">{L.T("Todas las semanas", "Every week")}</span>{string.Concat(chips)}</div>""");
        sb.Append($"""<button class="wtoggle" type="button" aria-expanded="false" aria-controls="weeks" data-open="{H(L.T("Ver todas las semanas", "See every week"))}" data-close="{H(L.T("Ocultar las semanas", "Hide the weeks"))}">{L.T("Ver todas las semanas", "See every week")}</button>""");
        sb.Append("</div></section>");

        // Las cinco semanas, en una fila.
        var weeks = new StringBuilder("""<section class="weeks" id="weeks" hidden><div class="wgrid">""");
        for (var w = 1; w <= Prizes.WeekStarts.Length; w++)
        {
            var list = Prizes.All.Where(p => p.Week == w).ToList();
            var body = list.Count > 0 ? $"<ul>{string.Concat(list.Select(Line))}</ul>" : $"<p class=\"sm\">{L.T("Premios aún no anunciados.", "Prizes not announced yet.")}</p>";
            weeks.Append($"""<div class="wk{(w == thisWeek ? " now" : "")}"><div class="wh"><b>{L.F("Semana {0}", "Week {0}", w)}</b>{(w == thisWeek ? $"<span class=\"tag gold\">{L.T("Esta semana", "This week")}</span>" : "")}<span class="wr">{Range(w)}</span></div>{body}</div>""");
        }
        weeks.Append("</div></section>");
        return weeks.ToString();
    }

    /// <summary>
    /// Pestaña «Compañeros» (opción de Ajustes): con quién juegas, partidas juntos, victorias y cofres que se les vio
    /// abrir. Los cofres salen de tu replay, que solo tiene lo que pasó cerca de ti, así que son un mínimo.
    /// Devuelve el HTML y cuántos compañeros habituales (o mostrados) hay, para la etiqueta de la pestaña.
    /// </summary>
    private static (string Html, int Count) TeammatesPanel(List<MatchRecord> matches, IReadOnlyCollection<string> extraKinds, Func<int, string> N)
    {
        int Seen(TeammateRecord t) => MatchRecord.Seen(t, extraKinds);
        var all = matches
            .Where(m => m.Teammates != null)
            .SelectMany(m => m.Teammates!.Select(t => (m, t)))
            .GroupBy(r => r.t.EpicId ?? r.t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var latest = g.OrderByDescending(r => When(r.m)).First();
                var together = g.Select(r => r.m).Distinct().ToList();
                return (
                    Key: g.Key,
                    Name: latest.t.Name,
                    Matches: together.Count,
                    Chests: g.Sum(r => Seen(r.t)),
                    Mine: together.Sum(m => m.OwnChests),
                    Best: g.Max(r => Seen(r.t)),
                    Wins: together.Count(m => m.Placement == 1),
                    Last: When(latest.m),
                    Team: together.Sum(m => m.OwnChests + m.TeammatesSeen(extraKinds)));
            })
            .OrderByDescending(r => r.Matches).ThenByDescending(r => r.Chests)
            .ToList();

        var sb = new StringBuilder();
        var tracked = matches.Count(m => m.Teammates is { Count: > 0 });
        string[] tones = { "var(--t1)", "var(--t2)", "var(--t3)", "var(--t4)", "var(--t5)" };
        string Tone(string key) => tones[(int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(key) % tones.Length)];
        string Initial(string name) => H(name.Trim().Length > 0 ? char.ToUpperInvariant(name.Trim()[0]).ToString() : "?");
        string Pct(int part, int whole) => whole > 0 ? (100.0 * part / whole).ToString("0", Loc) + " %" : "—";
        if (all.Count == 0)
        {
            sb.Append($"""<div class="card"><div class="empty big">{L.T("Aún no hay partidas en equipo analizadas. Aparecerán aquí tras tu próxima partida en dúos, tríos o escuadrones.", "No team matches analyzed yet. They will show up here after your next duos, trios or squads match.")}</div></div>""");
            return (sb.ToString(), 0);
        }

        // Cofres del equipo: tú + lo que se vio abrir a tus compañeros, en las partidas con compañeros.
        var teamMatches = matches.Where(m => m.Teammates is { Count: > 0 }).ToList();
        var teamMine = teamMatches.Sum(m => m.OwnChests);
        var teamMates = teamMatches.Sum(m => m.TeammatesSeen(extraKinds));
        var teamAll = teamMine + teamMates;
        string Avg(int total, int count) => count > 0 ? ((double)total / count).ToString("0.0", Loc) : "—";
        sb.Append($"""
            <section><div class="sh"><h2>{L.T("Cofres del equipo", "Team chests")}</h2></div>
            <div class="minis">
              <div class="mini-kpi"><b class="cy"><small>≥</small>{N(teamAll)}</b><span>{L.T("cofres del equipo en total", "team chests in total")}</span></div>
              <div class="mini-kpi"><b><small>≥</small>{Avg(teamAll, teamMatches.Count)}</b><span>{L.T("del equipo por partida en equipo", "for the team per team match")}</span></div>
              <div class="mini-kpi"><b>{Avg(teamMine, teamMatches.Count)}</b><span>{L.T("tuyos por partida en equipo", "yours per team match")}</span></div>
              <div class="mini-kpi"><b><small>≥</small>{(teamAll > 0 ? (100.0 * teamMates / teamAll).ToString("0", Loc) : "0")} %</b><span>{L.T("cofres del equipo abiertos por tus compañeros", "team chests opened by your teammates")}</span></div>
            </div></section>
            """);

        // Todos tus compañeros (una tabla que se ordena por cualquier columna: partidas, victorias, media…) y, al lado,
        // las dos clasificaciones que la tabla no enseña de un vistazo.
        var medals = new[] { "🥇", "🥈", "🥉" };
        string Games(int n) => L.Count(n, "partida", "partidas", "match", "matches");
        string Ranking(string title, List<(string Key, string Name, double Value, string Text, string Sub)> items)
        {
            if (items.Count == 0)
            {
                return "";
            }
            var max = Math.Max(0.0001, items.Max(i => i.Value));
            var r = new StringBuilder($"<section class=\"grow\"><div class=\"sh\"><h2>{title}</h2><span class=\"meta\">Top 5</span></div><div class=\"card rank one\"><ol>");
            for (var i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var rank = i < medals.Length ? $"<span class=\"rk medal\" aria-label=\"{i + 1}\">{medals[i]}</span>" : $"<span class=\"rk\">{i + 1}</span>";
                var w = (100.0 * it.Value / max).ToString("0.##", CultureInfo.InvariantCulture);
                r.Append($"<li{(i == 0 ? " class=\"first t\"" : "")}>{rank}<span class=\"av sm-av\" style=\"background:{Tone(it.Key)}\">{Initial(it.Name)}</span>"
                    + $"<span class=\"who2\"><span class=\"nm\" title=\"{H(it.Name)}\">{H(it.Name)}</span><span class=\"sub\">{it.Sub}</span></span><b>{it.Text}</b>"
                    + $"<span class=\"bar\"><i style=\"width:{w}%\"></i></span></li>");
            }
            r.Append("</ol></div></section>");
            return r.ToString();
        }

        // Los habituales (2 partidas o más); los de relleno con una sola partida se resumen en una línea.
        var rows = all.Where(r => r.Matches >= 2).ToList();
        var oneOff = all.Count - rows.Count;
        if (rows.Count == 0)
        {
            rows = all.Take(20).ToList();
            oneOff = Math.Max(0, all.Count - 20);
        }
        string V(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        string Th(string label, string type, bool right, bool current = false) =>
            $"<th{(right ? " class=\"c\"" : "")} data-type=\"{type}\"{(current ? " aria-sort=\"descending\"" : "")}><button type=\"button\">{label}</button></th>";
        sb.Append("<div class=\"duo wide\"><section class=\"fill\">");
        sb.Append($"""
            <div class="sh"><h2>{L.T("Todos tus compañeros", "All your teammates")}</h2><span class="meta">{L.Count(all.Count, "compañero", "compañeros", "teammate", "teammates")} · {L.T("toca una columna para ordenar", "tap a column to sort")}</span></div>
            <div class="card tw"><table class="sortable"><thead><tr>{Th(L.T("Compañero", "Teammate"), "s", false)}{Th(L.T("Partidas", "Matches"), "n", true, true)}{Th(L.T("Victorias", "Wins"), "n", true)}{Th(L.T("Cofres abiertos", "Chests opened"), "n", true)}{Th(L.T("Media", "Average"), "n", true)}{Th(L.T("Aporta", "Share"), "n", true)}{Th(L.T("Última partida", "Last match"), "n", true)}</tr></thead><tbody>
            """);
        foreach (var r in rows)
        {
            var avg = (double)r.Chests / r.Matches;
            var share = r.Team > 0 ? (double)r.Chests / r.Team : 0;
            var wins = r.Wins > 0 ? $"<b class=\"acc\">{N(r.Wins)}</b> <span class=\"sm\">· {Pct(r.Wins, r.Matches)}</span>" : "<span class=\"muted\">—</span>";
            sb.Append($"<tr data-row><td data-v=\"{H(r.Name.ToLowerInvariant())}\"><div class=\"who\"><span class=\"av sm-av\" style=\"background:{Tone(r.Key)}\">{Initial(r.Name)}</span><span class=\"nm\">{H(r.Name)}</span></div></td>"
                + $"<td class=\"c\" data-v=\"{r.Matches}\">{N(r.Matches)}</td><td class=\"c nw\" data-v=\"{V(r.Wins + r.Wins / 1000.0 / r.Matches)}\">{wins}</td><td class=\"c nw\" data-v=\"{r.Chests}\"><span class=\"sm\">≥</span> <b>{N(r.Chests)}</b></td>"
                + $"<td class=\"c\" data-v=\"{V(avg)}\"><b>{avg.ToString("0.0", Loc)}</b></td>"
                + $"<td class=\"c nw\" data-v=\"{V(share)}\"><span class=\"sm\">≥</span> {Pct(r.Chests, r.Team)}</td><td class=\"c nw sm\" data-v=\"{r.Last.Ticks}\">{L.DateTimeShort(r.Last)}</td></tr>");
        }
        sb.Append("</tbody>");
        if (oneOff > 0)
        {
            var more = L.Count(oneOff, "compañero", "compañeros", "teammate", "teammates");
            sb.Append($"<tfoot><tr><td colspan=\"7\" class=\"sm foot\">{(all.Count(r => r.Matches >= 2) > 0
                ? L.F("Y {0} con una sola partida (relleno de equipo), no mostrados.", "And {0} with a single match (team fill), not shown.", more)
                : L.F("Y {0} más.", "And {0} more.", more))}</td></tr></tfoot>");
        }
        sb.Append("</table></div></section><div class=\"stack\">");
        sb.Append(Ranking(L.T("Más cofres abiertos", "Most chests opened"),
            all.OrderByDescending(r => r.Chests).ThenByDescending(r => r.Matches).Take(5)
                .Select(r => (r.Key, r.Name, (double)r.Chests, $"<small>≥</small>{N(r.Chests)}", Games(r.Matches))).ToList()));
        sb.Append(Ranking(L.T("Récord en una partida", "Best single match"),
            all.Where(r => r.Best > 0).OrderByDescending(r => r.Best).ThenByDescending(r => r.Matches).Take(5)
                .Select(r => (r.Key, r.Name, (double)r.Best, $"<small>≥</small>{N(r.Best)}", Games(r.Matches))).ToList()));
        sb.Append("</div></div>");
        return (sb.ToString(), rows.Count);
    }

    /// <summary>Tope "redondo" para la escala del gráfico (1, 2, 2.5, 5 × 10ⁿ), siempre ≥ el valor.</summary>
    private static int NiceMax(int value)
    {
        if (value <= 4)
        {
            return 4;
        }
        var mag = Math.Pow(10, Math.Floor(Math.Log10(value)));
        foreach (var step in new[] { 1, 2, 2.5, 5, 10 })
        {
            if (step * mag >= value)
            {
                var nice = (int)Math.Round(step * mag);
                return nice % 2 == 0 ? nice : nice + 1;
            }
        }
        return value;
    }

    private static string LoadingCard(ScanProgress? p)
    {
        if (p == null)
        {
            return "";
        }
        var pct = p.Total > 0 ? (int)Math.Round(100.0 * p.Done / p.Total) : 0;
        var head = L.F("Analizando tus replays… {0} / {1}", "Analyzing your replays… {0} / {1}", p.Done, p.Total);
        var note = L.T("Esta página se actualiza sola: las cifras de abajo aumentan a medida que se leen los replays.",
            "This page updates by itself: the numbers below grow as the replays are read.");
        return $"""<div class="loading"><div class="spin"></div><div class="grow"><b>{head}</b><div class="pbar"><i style="width:{pct}%"></i></div><span class="muted">{note}</span></div></div>""";
    }

    private static string ProfilesLine(IReadOnlyList<Profile> profiles, string player)
    {
        var others = profiles.Where(p => p.Name != player).Select(p => H(p.Name)).ToList();
        if (others.Count == 0)
        {
            return "";
        }
        return $"""<div class="profiles">{L.T("Otros perfiles en este PC: ", "Other profiles on this PC: ")}{string.Join(", ", others)} · {L.T("cambia de perfil en la configuración de la app", "switch profile in the app settings")}</div>""";
    }

    /// <summary>Rewrites only the OBS overlay page (called on every live update).</summary>
    public static void WriteOverlay(Store store, OverlayView view)
    {
        try
        {
            WriteAtomic(store.OverlayPath, Overlay(view));
        }
        catch
        {
            // the page is a convenience: never stop the tracker for it
        }
    }

    private const string OverlayStyle = """
        html,body{margin:0;background:transparent;color:#e6eaf2;font:500 16px/1.25 "Segoe UI",system-ui,sans-serif}
        .box{position:relative;display:inline-block;min-width:230px;padding:10px 14px 10px 18px;border-radius:14px;overflow:hidden;
          background:rgba(15,19,27,.9);border:1px solid #2a3344;box-shadow:0 6px 24px rgba(0,0,0,.35)}
        .box:before{content:"";position:absolute;left:0;top:0;bottom:0;width:4px;background:#ffb84d}
        .box.live:before{background:#3ddc97}
        .head{display:flex;align-items:center;justify-content:space-between;gap:16px;font:700 12px "Segoe UI",sans-serif;letter-spacing:.12em;color:#7a8496}
        .pill{display:inline-flex;align-items:center;gap:6px;padding:3px 10px;border-radius:99px;background:#143a2c;color:#3ddc97;letter-spacing:.06em}
        .pill:before{content:"";width:8px;height:8px;border-radius:50%;background:#3ddc97;animation:pulse 1.6s ease-in-out infinite}
        @keyframes pulse{50%{opacity:.35}}
        .main{display:flex;align-items:center;justify-content:space-between;gap:16px;margin:6px 0 8px}
        .big{display:block;font-size:34px;font-weight:800;line-height:1.05;font-variant-numeric:tabular-nums}
        .total .big{color:#ffb84d}
        .cap{display:block;font-size:11px;font-weight:700;letter-spacing:.1em;color:#7a8496;text-transform:uppercase}
        .chip{min-width:84px;padding:5px 12px;border-radius:11px;background:#1a202d;text-align:center}
        .chip.live .big,.chip.live .cap{color:#3ddc97}.chip.none .big{color:#505868}
        .stats{display:flex;justify-content:space-between;gap:14px;border-top:1px solid #222938;padding-top:7px}
        .stat .cap{font-size:10px}.stat b{display:block;font-size:16px;font-weight:700}
        .stat.t b{color:#ff80aa}.stat small{display:block;font-size:11px;font-weight:600;color:#7a8496}
        """;

    /// <summary>The overlay card (shared by overlay.html and the OBS page served by the tool).</summary>
    public static string OverlayBox(OverlayView v)
    {
        var stats = new StringBuilder();
        foreach (var r in v.Rows)
        {
            stats.Append($"<div class=\"stat{(r.Treasure ? " t" : "")}\"><span class=\"cap\">{H(r.Label)}</span><b>{H(r.Value)}</b></div>");
        }
        var badge = v.Badge != null ? $"<span class=\"pill\">{H(v.Badge.TrimStart('●', ' '))}</span>" : "";
        var chip = v.Current == null ? "none" : v.CurrentLive ? "live" : "";
        return $"""
            <div class="box{(v.CurrentLive ? " live" : "")}">
            <div class="head"><span>{L.T("COFRES", "CHESTS")}</span>{badge}</div>
            <div class="main">
              <div class="total"><span class="big">{H(v.Total)}</span><span class="cap">total</span></div>
              <div class="chip {chip}"><span class="big">{H(v.Current ?? "—")}</span><span class="cap">{H(v.CurrentCaption)}</span></div>
            </div>
            <div class="stats">{stats}</div>
            </div>
            """;
    }

    /// <summary>overlay.html (a file: reloads itself every 10 s).</summary>
    public static string Overlay(OverlayView v) => $"""
        <!doctype html><html lang="{L.Code}"><head><meta charset="utf-8"><meta http-equiv="refresh" content="10">
        <title>{L.T("Overlay de cofres", "Chest overlay")}</title><style>{OverlayStyle}</style></head>
        <body>{OverlayBox(v)}</body></html>
        """;

    /// <summary>OBS page served by the tool: fetches the card every 2 s and swaps it only when it changed (no flicker).</summary>
    public static string OverlayLivePage(string box) => $$"""
        <!doctype html><html lang="{{L.Code}}"><head><meta charset="utf-8">
        <title>{{L.T("Overlay de cofres", "Chest overlay")}}</title><style>{{OverlayStyle}}</style></head>
        <body><div id="o">{{box}}</div>
        <script>
        let last = document.getElementById("o").innerHTML;
        setInterval(async () => {
          try {
            const r = await fetch("/view", { cache: "no-store" });
            if (!r.ok) return;
            const html = await r.text();
            if (html !== last) { document.getElementById("o").innerHTML = html; last = html; }
          } catch (e) { /* tool closed: keep the last figures */ }
        }, 2000);
        </script></body></html>
        """;

    public static string Csv(TrackerData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine(L.T("fecha;modo;jugador;puesto;jugadores;cofres;cajas_municion;tesoros;archivo",
            "date;mode;player;placement;players;chests;ammo_boxes;treasures;file"));

        foreach (var m in data.Matches.Where(Profiles.IsSupportedVersion).OrderBy(When))
        {
            var t = string.Join(" + ", m.TreasureFinds.Select(f => f.DisplayName));
            sb.AppendLine(string.Join(";",
                When(m).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                Cell(Pretty.Playlist(m.Playlist)), Cell(m.PlayerName), m.Placement, m.TotalPlayers,
                m.ChestsOpened, m.AmmoBoxesOpened, Cell(t), Cell(m.ReplayFile)));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Texto seguro para una celda: sin separadores ni saltos de línea, y sin que Excel lo lea como fórmula.
    /// </summary>
    private static string Cell(string? value)
    {
        var s = (value ?? "").Replace(';', ',').Replace('\r', ' ').Replace('\n', ' ').Replace('"', '\'');
        var trimmed = s.TrimStart();
        return trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '|' ? "'" + s : s;
    }
}
