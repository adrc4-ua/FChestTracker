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
            var view = Profiles.View(data, config);
            WriteAtomic(store.ReportPath, Report(config, view, Profiles.List(data, config),Profiles.InitialChests(data, config)));
            WriteAtomic(store.OverlayPath, Overlay(OverlayModel.Build(view, null, false, Profiles.InitialChests(data, config))));
            WriteAtomic(store.CsvPath, Csv(data));
        }
        catch (Exception ex)
        {
            Console.WriteLine(L.F("No se pudo escribir el informe: {0}", "Cannot write the report: {0}", ex.Message));
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
          background:radial-gradient(900px 420px at 50% -160px,rgba(232,176,75,.13),transparent 70%),var(--bg);background-attachment:fixed;min-height:100vh}
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
        .bn b{font-size:16px;letter-spacing:-.01em}
        .main{min-width:0;padding:26px 30px 34px}
        .ph{display:flex;align-items:center;gap:14px;margin:0 0 20px;padding-bottom:16px;border-bottom:1px solid var(--line2)}
        .phi{width:42px;height:42px;flex:none;border-radius:12px;display:grid;place-items:center;color:var(--gold);background:var(--gold-soft);border:1px solid var(--gold-line)}
        .phi svg{width:22px;height:22px}
        .ph h1{margin:0;font-size:24px;line-height:1.2;font-weight:700;letter-spacing:-.02em}
        .ph p{margin:3px 0 0;font-size:13px;color:var(--muted)}
        .ph p::first-letter{text-transform:uppercase}

        /* Cifras clave */
        .kpis{display:grid;grid-template-columns:minmax(0,1.9fr) repeat(5,minmax(0,1fr));gap:12px}
        .kpi,.card,.mini-kpi,.mate,.board{background:linear-gradient(180deg,rgba(255,255,255,.022),rgba(255,255,255,0) 60%),var(--card);border:1px solid var(--line);border-radius:var(--radius);box-shadow:var(--shadow)}
        .kpi{position:relative;padding:16px 18px;min-width:0;display:flex;flex-direction:column}
        .kpi .ki{display:inline-flex;color:var(--gold);margin-bottom:10px}
        .kpi .l{font-size:12.5px;font-weight:600;color:var(--muted)}
        .kpi .v{margin-top:2px;font-size:28px;font-weight:700;line-height:1.15;letter-spacing:-.025em;font-variant-numeric:tabular-nums;overflow-wrap:anywhere}
        .kpi .v.gold{color:var(--gold)}
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

        /* Evolución de cofres: curva a todo el ancho, textos y puntos encima */
        .evo{position:relative;height:clamp(140px,22vh,240px);margin:6px 8px 26px 44px}
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
        .duo>section{margin-top:0}
        .stack{display:flex;flex-direction:column;gap:16px;min-width:0}
        .side-card{margin:0;padding:16px 18px}
        .tl{list-style:none;margin:0;padding:0}
        .tl li{display:flex;align-items:center;gap:12px;padding:8px 0;border-top:1px solid var(--line)}
        .tl li:first-child{border-top:0;padding-top:2px}
        .gem{width:34px;height:34px;flex:none;border-radius:10px;display:grid;place-items:center;color:var(--muted);background:var(--bg2);border:1px solid var(--line2)}
        .gem.f{color:var(--gold-hi);background:var(--gold-soft);border-color:var(--gold-line)}
        .tn{display:flex;flex-direction:column;min-width:0}
        .tn b{font-size:13.5px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .tn small{font-size:12px;color:var(--muted);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .team2{display:grid;grid-template-columns:1fr 1fr;gap:16px}
        .team2>div{display:flex;flex-direction:column;gap:4px;min-width:0}
        .team2>div+div{padding-left:16px;border-left:1px solid var(--line2)}
        .team2 b{font-size:24px;line-height:1.15;color:var(--gold)}
        .team2 b.nm{font-size:15px;color:var(--ink);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .team2 .track{display:block;height:5px;margin-top:4px}
        .gemc{display:inline-flex;align-items:center;gap:5px;color:var(--gold);font-weight:600}
        table.compact th,table.compact td{padding:9px 14px}
        .trow{display:grid;grid-template-columns:minmax(0,1.6fr) minmax(0,1fr);gap:16px;align-items:start}
        .trow>section{margin-top:22px}
        .trow .tiles{grid-template-columns:repeat(auto-fill,minmax(140px,1fr))}

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
        th{position:sticky;top:0;background:var(--card2);font-size:11px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;color:var(--muted);white-space:nowrap}
        thead tr th:first-child{border-top-left-radius:17px} thead tr th:last-child{border-top-right-radius:17px}
        tbody tr:last-child td{border-bottom:none}
        tbody tr{transition:background .12s}
        tbody tr:hover td{background:rgba(232,176,75,.035)}
        .r{text-align:right} .nw{white-space:nowrap}
        .sm{font-size:12.5px;color:var(--muted)}
        .badge{display:inline-block;line-height:1.5;padding:2px 10px;border-radius:99px;font-size:12.5px;font-weight:600;white-space:nowrap;background:var(--bg2);color:var(--muted);border:1px solid var(--line2)}
        .badge.win{background:var(--gold-soft);color:var(--gold-hi);border-color:var(--gold-line)}
        .badge.top{background:var(--champ-soft);color:var(--champ);border-color:rgba(216,194,143,.25)}
        .badge.mode{background:transparent;border:1px solid var(--line2);color:var(--ink);font-weight:500}
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
        .tile .won{position:absolute;top:10px;right:12px;font-size:12px;font-weight:700;color:#1a1305;background:var(--gold);border-radius:99px;padding:0 8px}
        .hunt-note{margin:0}
        .always{display:flex;flex-wrap:wrap;align-items:center;gap:6px 8px;margin-top:14px;padding-top:14px;border-top:1px solid var(--line)}
        .always .al{font-size:11px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;color:var(--muted);margin-right:4px}
        .pchip{display:inline-flex;align-items:center;gap:5px;padding:3px 10px;border-radius:99px;font-size:12.5px;background:var(--bg2);border:1px solid var(--line2);color:var(--ink);white-space:nowrap}
        .pchip.got{color:var(--gold-hi);background:var(--gold-soft);border-color:var(--gold-line)}
        .weeks{margin-top:12px}
        .weeks summary{cursor:pointer;list-style:none;display:inline-flex;align-items:center;gap:8px;font-size:13px;font-weight:600;color:var(--gold);padding:4px 10px;margin-left:-10px;border-radius:8px}
        .weeks summary::-webkit-details-marker{display:none}
        .weeks summary::before{content:"▸";transition:transform .15s}
        .weeks[open] summary::before{transform:rotate(90deg)}
        .weeks summary:hover{background:var(--gold-soft)}
        .wgrid{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:10px;margin-top:10px}
        .wk{padding:12px 14px;border-radius:14px;background:var(--bg2);border:1px solid var(--line)}
        .wk.now{border-color:var(--gold-line)}
        .wh{display:flex;flex-wrap:wrap;align-items:center;gap:4px 8px;margin-bottom:6px}
        .wh b{font-size:13.5px}
        .wh span:not(.tag){font-size:12px;color:var(--muted)}
        .wk ul{list-style:none;margin:0;padding:0}
        .wk li{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:0 8px;padding:6px 0;border-top:1px solid var(--line)}
        .wk li span{font-size:13px;font-weight:600;overflow-wrap:anywhere}
        .wk li small{grid-column:1;font-size:11.5px;color:var(--muted)}
        .wk li b{grid-column:2;grid-row:1/3;align-self:center;color:var(--gold)}
        .wk li.got span{color:var(--gold-hi)}
        .wk p.sm{margin:4px 0}

        /* Carga */
        .loading{display:flex;gap:16px;align-items:center;background:var(--card);border:1px solid var(--gold-line);border-radius:var(--radius);padding:16px 18px;margin-bottom:22px;box-shadow:var(--shadow)}
        .spin{width:30px;height:30px;flex:none;border:3px solid var(--line2);border-top-color:var(--gold);border-radius:50%;animation:spin .8s linear infinite}
        @keyframes spin{to{transform:rotate(360deg)}}
        .loading .grow{flex:1;min-width:0}
        .pbar{height:6px;background:var(--line2);border-radius:4px;overflow:hidden;margin:8px 0 6px}
        .pbar i{display:block;height:100%;background:linear-gradient(90deg,var(--gold-lo),var(--gold-hi));border-radius:4px}


        /* Menú lateral: sin JavaScript se ven todas las secciones seguidas */
        .nav{display:flex;flex-direction:column;gap:4px}
        html:not(.js) .nav{display:none}
        .tab{appearance:none;border:0;background:transparent;color:var(--muted);font:inherit;font-size:14.5px;font-weight:600;min-height:44px;width:100%;padding:10px 14px;border-radius:12px;
          cursor:pointer;display:flex;align-items:center;gap:12px;text-align:left;white-space:nowrap;transition:background .15s,color .15s}
        .tab .ti{flex:none;opacity:.85}
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
        .mates{display:grid;grid-template-columns:repeat(auto-fill,minmax(270px,1fr));gap:12px}
        .mate{padding:16px 18px;min-width:0}
        .who{display:flex;align-items:center;gap:12px;min-width:0}
        .who .grow{min-width:0}
        .av{width:40px;height:40px;flex:none;border-radius:50%;display:grid;place-items:center;font-weight:700;font-size:17px;color:#1a1305}
        .av.sm-av{width:26px;height:26px;font-size:12px}
        .who .nm{font-weight:700;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
        .mate .nm{font-size:15.5px}
        .mate .st{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;margin-top:14px;padding-top:12px;border-top:1px solid var(--line2)}
        .mate .st b{display:block;font-size:20px;line-height:1.2;font-variant-numeric:tabular-nums}
        .mate .st b small{font-size:12px;font-weight:600;color:var(--muted);margin-right:2px}
        .mate .st span{display:block;font-size:11.5px;color:var(--muted);line-height:1.35}
        .boards{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:12px}
        .board{padding:14px 16px 12px;min-width:0}
        .board h3{margin:0 0 8px;font-size:11px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;color:var(--muted)}
        .board ol{list-style:none;margin:0;padding:0}
        .board li{display:grid;grid-template-columns:22px 26px minmax(0,1fr) auto;align-items:center;gap:2px 8px;padding:5px 0;border-bottom:1px solid var(--line)}
        .seg{display:flex;flex-wrap:wrap;gap:4px;padding:3px;border:1px solid var(--line);border-radius:10px;background:var(--bg2)}
        .seg button{font:inherit;font-size:13px;font-weight:600;color:var(--muted);background:none;border:0;border-radius:7px;padding:6px 12px;cursor:pointer}
        .seg button:hover{color:var(--text)}
        .seg button[aria-selected="true"]{color:#1a1206;background:linear-gradient(180deg,var(--gold-hi),var(--gold))}
        .seg button:focus-visible{outline:2px solid var(--gold-hi);outline-offset:2px}
        .board li:last-child{border-bottom:none}
        .board .rk{font-size:12px;font-weight:700;color:var(--faint);text-align:center;font-variant-numeric:tabular-nums}
        .board .rk.medal{font-size:16px}
        .board .who2{display:flex;flex-direction:column;min-width:0}
        .board .who2 .nm{font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
        .board .who2 .sub{font-size:11.5px;color:var(--muted);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .board li b{font-size:15px;font-variant-numeric:tabular-nums;white-space:nowrap}
        .board li b small{font-size:11px;font-weight:600;color:var(--muted);margin-right:2px}
        .board li.first b{color:var(--gold-hi);font-size:17px}
        .board .bar{grid-column:3/-1;height:4px;border-radius:2px;background:var(--bg2);overflow:hidden;box-shadow:none}
        .board .bar i{display:block;height:100%;background:linear-gradient(90deg,var(--gold-lo),var(--gold));border-radius:2px}
        .board li.first .bar i{background:linear-gradient(90deg,var(--gold),var(--gold-hi))}
        .bfoot{margin-top:8px;font-size:12px;color:var(--muted)}
        .mates-line{display:flex;flex-wrap:wrap;gap:4px 6px;margin-top:5px}
        .chip{display:inline-flex;align-items:center;gap:5px;padding:2px 9px 2px 3px;border-radius:99px;background:var(--bg2);border:1px solid var(--line);font-size:12px;white-space:nowrap;max-width:100%}
        .matches .chip{padding-left:9px}
        .chip .cn{color:var(--muted);overflow:hidden;text-overflow:ellipsis;max-width:130px}
        .chip b{font-variant-numeric:tabular-nums}
        .chip .sm-av{width:20px;height:20px;font-size:10.5px}
        .mini-kpi b small{font-size:13px;font-weight:600;color:var(--muted);margin-right:2px}
        .teambar{display:flex;align-items:center;justify-content:flex-end;gap:10px}
        .teambar .tb{position:relative;width:90px;height:5px;border-radius:3px;background:var(--bg2);overflow:hidden}
        .teambar .tb i{position:absolute;left:0;top:0;bottom:0;border-radius:3px;background:var(--gold-lo)}
        .teambar .tb i.me{background:var(--gold);z-index:1}
        .teambar b{min-width:44px;text-align:right}
        .teambar b small{font-size:11px;color:var(--muted);margin-right:2px}
        @media (max-width:520px){.teambar .tb{display:none}}
        @media (max-width:860px){.minis{grid-template-columns:repeat(2,minmax(0,1fr))}.grid2,.trow{grid-template-columns:1fr}}
        @media (max-width:1280px){.kpis{grid-template-columns:repeat(5,minmax(0,1fr))}.kpi.hero{grid-column:1/-1}}
        @media (max-width:1100px){.duo{grid-template-columns:1fr}}
        /* Móvil y ventanas estrechas: el menú pasa arriba y la página se desplaza normalmente */
        @media (max-width:999px){.app{grid-template-columns:1fr}.side{position:sticky;z-index:20;height:auto;flex-direction:row;align-items:center;gap:10px;padding:10px 12px;
          border-right:0;border-bottom:1px solid var(--line2);background:rgba(11,11,13,.92);-webkit-backdrop-filter:blur(12px);backdrop-filter:blur(12px)}
          .brand img{width:36px;height:36px}.bn{display:none}.nav{flex-direction:row;overflow-x:auto;scrollbar-width:none}.nav::-webkit-scrollbar{display:none}
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
          .duo>.stack{min-height:0;overflow:auto;padding-right:2px}
          .main>.panel:not([hidden]){overflow:auto}
          .segsec .segp:not([hidden]){flex:0 1 auto;min-height:0;overflow:auto}
          .board ol{max-height:clamp(150px,28vh,300px);overflow:auto;padding-right:4px}
          .trow>section>.card{max-height:42vh;overflow:auto}
        }
        .kpi .s::first-letter,.split span::first-letter,.mini-kpi span::first-letter,.sh .meta::first-letter,.mate .st span::first-letter,
        .board .who2 .sub::first-letter,.bfoot::first-letter,.row>span::first-letter,.badge::first-letter,
        .chests+.sm::first-letter,td>.sm::first-letter,.tag::first-letter,.pchip::first-letter{text-transform:uppercase}
        @media (prefers-reduced-motion:reduce){.dot,.spin{animation:none}}
        """;

    // Pestañas del informe. Se abre siempre en Resumen; la pestaña elegida y la posición solo se recuerdan en esa
    // pestaña del navegador mientras la página se recarga sola. Sin JavaScript se ven todas las secciones seguidas.
    private const string TabsScript = """
        (function(){var d=document,tabs=[].slice.call(d.querySelectorAll('[data-tab]'));if(!tabs.length)return;d.documentElement.classList.add('js');
        function get(k,s){try{return (s?sessionStorage:localStorage).getItem(k)}catch(e){return null}}
        function put(k,v,s){try{(s?sessionStorage:localStorage).setItem(k,v)}catch(e){}}
        function show(id,save){var t=null;tabs.forEach(function(b){var on=b.getAttribute('data-tab')===id;b.setAttribute('aria-selected',on?'true':'false');b.tabIndex=on?0:-1;if(on)t=b});
          if(!t)return false;var bar=t.parentNode;bar.scrollLeft=t.offsetLeft-(bar.clientWidth-t.offsetWidth)/2;[].forEach.call(d.querySelectorAll('.panel'),function(p){p.hidden=p.id!=='t-'+id});if(save)put('cc-tab',id,true);return true}
        var h=location.hash.slice(1);if(!(h&&show(h,true))&&!show(get('cc-tab',true)||'',false))show(tabs[0].getAttribute('data-tab'),false);
        function go(id){if(show(id,true)){window.scrollTo(0,0);try{history.replaceState(null,'','#'+id)}catch(e){}}}
        tabs.forEach(function(b,i){b.addEventListener('click',function(){go(b.getAttribute('data-tab'))});
          b.addEventListener('keydown',function(e){var k=e.key==='ArrowRight'||e.key==='ArrowDown'?1:e.key==='ArrowLeft'||e.key==='ArrowUp'?-1:0;if(!k)return;e.preventDefault();var n=tabs[(i+k+tabs.length)%tabs.length];go(n.getAttribute('data-tab'));n.focus()})});
        [].forEach.call(d.querySelectorAll('[data-go]'),function(a){a.addEventListener('click',function(e){e.preventDefault();go(a.getAttribute('data-go'))})});
        [].forEach.call(d.querySelectorAll('.segsec'),function(sec){var bar=sec.querySelector('.seg'),meta=sec.querySelector('.sh .meta'),ps=[].slice.call(sec.querySelectorAll('.segp')),bs=[];
          function pick(i){ps.forEach(function(p,j){p.hidden=j!==i;bs[j].setAttribute('aria-selected',j===i?'true':'false');bs[j].tabIndex=j===i?0:-1});meta.textContent=ps[i].getAttribute('data-meta');put('cc-seg',String(i),true)}
          ps.forEach(function(p,i){var b=d.createElement('button');b.type='button';b.setAttribute('role','tab');b.textContent=p.getAttribute('data-title');p.setAttribute('role','tabpanel');
            b.addEventListener('click',function(){pick(i)});b.addEventListener('keydown',function(e){var k=e.key==='ArrowRight'?1:e.key==='ArrowLeft'?-1:0;if(!k)return;e.preventDefault();var n=(i+k+ps.length)%ps.length;pick(n);bs[n].focus()});bar.appendChild(b);bs.push(b)});
          var s=+(get('cc-seg',true)||0);pick(s<ps.length?s:0)});
        var y=get('cc-y',true);if(y)window.scrollTo(0,+y);addEventListener('pagehide',function(){put('cc-y',String(window.scrollY),true)});
        })();
        """;

    private const string ChestIcon = """<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 10V8a4 4 0 0 1 4-4h8a4 4 0 0 1 4 4v2"/><rect x="3" y="10" width="18" height="10" rx="2"/><path d="M3 14h18"/><rect x="10" y="12" width="4" height="4" rx="1"/></svg>""";

    // Iconos de las pestañas (trazo, 18 px, color del texto).
    private const string IconSvg = """<svg class="ti" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private static readonly Dictionary<string, string> TabIcons = new()
    {
        ["resumen"] = IconSvg + """<rect x="3" y="3" width="7" height="9" rx="1.5"/><rect x="14" y="3" width="7" height="5" rx="1.5"/><rect x="14" y="12" width="7" height="9" rx="1.5"/><rect x="3" y="16" width="7" height="5" rx="1.5"/></svg>""",
        ["cofres"] = IconSvg + """<path d="M4 10V8a4 4 0 0 1 4-4h8a4 4 0 0 1 4 4v2"/><rect x="3" y="10" width="18" height="10" rx="2"/><path d="M3 14h18"/><rect x="10" y="12" width="4" height="4" rx="1"/></svg>""",
        ["tesoros"] = IconSvg + """<path d="M6 3h12l3 6-9 12L3 9z"/><path d="M3 9h18M9 3l3 6 3-6M12 21 9 9m3 12 3-12"/></svg>""",
        ["partidas"] = IconSvg + """<path d="M8 6h13M8 12h13M8 18h13"/><circle cx="3.5" cy="6" r="1"/><circle cx="3.5" cy="12" r="1"/><circle cx="3.5" cy="18" r="1"/></svg>""",
        ["companeros"] = IconSvg + """<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0"/><path d="M16 4.6a3.5 3.5 0 0 1 0 6.8M18.5 14a6.5 6.5 0 0 1 3 6"/></svg>""",
    };

    // Iconos de las cifras del resumen.
    private const string KpiSvg = """<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private static readonly Dictionary<string, string> KpiIcons = new()
    {
        ["wins"] = KpiSvg + """<path d="M8 21h8M12 17v4M7 4h10v5a5 5 0 0 1-10 0z"/><path d="M17 5h3v2a3 3 0 0 1-3 3M7 5H4v2a3 3 0 0 0 3 3"/></svg>""",
        ["avg"] = KpiSvg + """<path d="M4 20V11M10 20V5M16 20v-7M21 20H3"/></svg>""",
        ["best"] = KpiSvg + """<path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/></svg>""",
        ["day"] = KpiSvg + """<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/></svg>""",
    };
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
            sb.Append($"""<span class="al" style="top:{Py(Y(avg))}"><i>{H(L.T("media", "avg"))} {avg.ToString("0", Loc)}</i></span>""");
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
        sb.Append($"""<div class="card tw"><table class="compact"><thead><tr><th>{L.T("Fecha", "Date")}</th><th>{L.T("Modo", "Mode")}</th><th>{L.T("Puesto", "Placement")}</th><th class="r">{L.T("Elim.", "Elims")}</th><th class="r">{L.T("Cofres", "Chests")}</th><th class="r">{L.T("Tesoros", "Treasures")}</th></tr></thead><tbody>""");
        if (rows.Count == 0)
        {
            sb.Append($"<tr><td colspan=\"6\" class=\"empty\">{L.T("Aún no hay partidas analizadas. Juega una y aparecerá aquí.", "No matches analyzed yet. Play one and it will show up here.")}</td></tr>");
        }
        foreach (var m in rows)
        {
            var placeClass = m.Placement switch { 1 => " win", > 0 and <= 10 => " top", _ => "" };
            var place = m.Placement == 1 ? "🏆 " + H(Pretty.Placement(m)) : H(Pretty.Placement(m));
            var treasures = m.TreasureFinds.Count > 0 ? $"<span class=\"gemc\">{m.TreasureFinds.Count}{GemIcon}</span>" : "<span class=\"muted\">—</span>";
            sb.Append($"<tr{(m.Placement == 1 ? " class=\"won\"" : "")}><td class=\"nw\">{L.DateTimeShort(When(m))}</td><td class=\"sm\">{H(Pretty.Playlist(m.Playlist))}</td><td><span class=\"badge{placeClass}\">{place}</span></td>"
                + $"<td class=\"r\">{(m.Eliminations is int e ? N0(e) : "<span class=\"muted\">—</span>")}</td><td class=\"r\"><b>{N0(m.ChestsOpened)}</b></td><td class=\"r\">{treasures}</td></tr>");
        }
        sb.Append("</tbody></table></div>");
    }

    /// <summary>Últimos tesoros conseguidos (resumen): nombre, partida y tipo.</summary>
    private static string LatestTreasures(List<(MatchRecord m, TreasureFind f)> rows, Func<int, string> N)
    {
        var sb = new StringBuilder($"""<section class="card side-card"><div class="sh"><h2>{L.T("Últimos tesoros", "Latest treasures")}</h2>{(rows.Count > 0 ? $"<a class=\"more\" href=\"#tesoros\" data-go=\"tesoros\">{L.F("Ver los {0}", "See all {0}", N(rows.Count))} →</a>" : "")}</div>""");
        if (rows.Count == 0)
        {
            sb.Append($"<p class=\"sm\">{L.T("Aún no has encontrado tesoros.", "No treasures yet.")}</p></section>");
            return sb.ToString();
        }
        sb.Append("<ul class=\"tl\">");
        foreach (var (m, f) in rows.OrderByDescending(r => When(r.m)).ThenByDescending(r => r.f.ObtainedAtSeconds ?? -1).Take(5))
        {
            var (name, type, featured) = Prizes.Describe(f, When(m));
            var kind = featured ? (type != null ? $"{Prizes.Featured} · {type}" : Prizes.Featured) : type ?? L.T("Artilugio", "Gizmo");
            sb.Append($"<li><span class=\"gem{(featured ? " f" : "")}\">{GemIcon}</span><span class=\"tn\"><b>{H(name)}</b><small>{L.DateTimeShort(When(m))} · {H(kind)}</small></span></li>");
        }
        sb.Append("</ul></section>");
        return sb.ToString();
    }

    /// <summary>Compañeros en el resumen: cuánto aportan al equipo y con quién juegas más.</summary>
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
            <section class="card side-card"><div class="sh"><h2>{L.T("Compañeros", "Teammates")}</h2><a class="more" href="#companeros" data-go="companeros">{L.T("Ver todo", "See all")} →</a></div>
            <div class="team2"><div><span class="sm">{L.T("Aportan tus compañeros", "From your teammates")}</span><b>≥ {share.ToString("0", Loc)}&nbsp;%</b><span class="track"><i style="width:{share.ToString("0.#", CultureInfo.InvariantCulture)}%"></i></span></div>
            <div><span class="sm">{L.T("Con quién más juegas", "Who you play with most")}</span><b class="nm">{H(top.Name)}</b><span class="sm">{L.Count(top.Matches, "partida juntos", "partidas juntos", "match together", "matches together")}</span></div></div>
            </section>
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
            <meta http-equiv="refresh" content="{(progress != null ? 2 : 15)}">
            <title>{title}{(player.Length > 0 ? " · " + H(player) : "")}</title>{(logo != null ? $"<link rel=\"icon\" href=\"{logo}\">" : "")}<style>{Style}</style></head><body><div class="app">
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
        sb.Append($"""<nav class="nav" role="tablist" aria-label="{L.T("Secciones del informe", "Report sections")}" aria-orientation="vertical">""");
        foreach (var (id, label, count) in tabs)
        {
            var countText = count != null ? $"<span class=\"c\" aria-label=\"{H(L.F("{0} en total", "{0} in total", count))}\">{count}</span>" : "";
            sb.Append($"""<button class="tab" role="tab" type="button" id="tab-{id}" data-tab="{id}" aria-controls="t-{id}">{TabIcons.GetValueOrDefault(id)}<span>{label}</span>{countText}</button>""");
        }
        sb.Append("</nav></aside><main class=\"main\">");
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
              {Kpi(KpiIcons["wins"], L.T("Victorias", "Wins"), N(wins), wins > 0 ? L.F("{0} % de tus partidas", "{0} % of your matches", winRate.ToString("0", Loc)) : "—", " gold")}
              {Kpi(TabIcons["partidas"], L.T("Partidas", "Matches"), N(matches.Count), L.T("analizadas", "analyzed"))}
              {Kpi(KpiIcons["avg"], L.T("Media", "Average"), avg.ToString("0.0", Loc), L.T("cofres por partida", "chests per match"), " gold")}
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
        sb.Append($"""<div class="sh"><h2>{L.T("Últimas partidas", "Latest matches")}</h2>{(matches.Count > 5 ? $"<a class=\"more\" href=\"#partidas\" data-go=\"partidas\">{L.T("Ver todas", "See all")} →</a>" : "")}</div>""");
        AppendCompactMatches(sb, matches.Take(5).ToList());
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
        sb.Append($"<section><div class=\"sh\"><h2>{L.T("Otros contenedores", "Other containers")}</h2><span class=\"meta\">{L.T("se activan en Ajustes", "turned on in Settings")}</span></div><div class=\"card pad\"><div class=\"rows\">");
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
            .Select(g => (Mode: g.Key, Matches: g.Count(), Chests: g.Sum(m => m.ChestsOpened), Best: g.Max(m => m.ChestsOpened), Wins: g.Count(m => m.Placement == 1)))
            .OrderByDescending(x => x.Matches).ToList();
        if (byMode.Count > 0)
        {
            var modeMax = Math.Max(0.1, byMode.Max(x => (double)x.Chests / x.Matches));
            sb.Append($"""
                <section class="fill"><div class="sh"><h2>{L.T("Por modo de juego", "By game mode")}</h2><span class="meta">{L.Count(byMode.Count, "modo", "modos", "mode", "modes")}</span></div>
                <div class="card tw"><table><thead><tr><th>{L.T("Modo", "Mode")}</th><th class="r">{L.T("Partidas", "Matches")}</th><th class="r">{L.T("Media", "Average")}</th><th class="r">{L.T("Récord", "Best")}</th><th class="r">{L.T("Cofres", "Chests")}</th><th class="r">{L.T("Victorias", "Wins")}</th></tr></thead><tbody>
                """);
            foreach (var x in byMode)
            {
                var a = (double)x.Chests / x.Matches;
                var w = (100.0 * a / modeMax).ToString("0.##", CultureInfo.InvariantCulture);
                sb.Append($"<tr><td><span class=\"badge mode\">{H(x.Mode)}</span></td><td class=\"r\">{N(x.Matches)}</td><td><div class=\"chests\"><span class=\"mini\"><i style=\"width:{w}%\"></i></span><b>{a.ToString("0.0", Loc)}</b></div></td><td class=\"r\">{N(x.Best)}</td><td class=\"r\">{N(x.Chests)}</td><td class=\"r\">{(x.Wins > 0 ? $"<span class=\"badge win\">🏆 {N(x.Wins)}</span>" : "<span class=\"muted\">—</span>")}</td></tr>");
            }
            sb.Append("</tbody></table></div></section>");
        }
        sb.Append("</div>");

        // ================================ Tesoros ================================
        sb.Append("""<div class="panel" id="t-tesoros" role="tabpanel" aria-labelledby="tab-tesoros">""");
        sb.Append(PageHeader("tesoros", L.T("Tesoros", "Treasures"), L.T("Caza del tesoro y todo lo que has conseguido", "Treasure hunt and everything you got")));
        if (treasureRows.Count > 0)
        {
            var unusual = treasureRows.Where(r => !r.f.SourceIsChest).ToList();
            if (unusual.Count > 0)
            {
                var kinds = H(string.Join(", ", unusual.Select(r => L.Name(r.f.SourceKind)).Distinct()));
                sb.Append($"<div class=\"flag\"><span class=\"ic\">⚑</span><div><b>{L.Count(unusual.Count, "tesoro obtenido", "tesoros obtenidos", "treasure obtained", "treasures obtained")} {L.T("de otra forma que con un cofre", "other than from a chest")}</b>: {kinds}. {L.T("Resaltados en la tabla de abajo.", "Highlighted in the table below.")}</div></div>");
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
            AppendTreasureHunt(sb, treasureRows);

            var bySource = treasureRows.GroupBy(r => L.Name(r.f.SourceKind)).OrderByDescending(g => g.Count()).ToList();
            var srcMax = Math.Max(1, bySource.Max(g => g.Count()));
            sb.Append($"<section><div class=\"sh\"><h2>{L.T("De dónde vienen los tesoros", "Where the treasures come from")}</h2><span class=\"meta\">{L.Count(treasureRows.Count, "tesoro", "tesoros", "treasure", "treasures")}</span></div><div class=\"card pad\"><div class=\"rows\">");
            foreach (var g in bySource)
            {
                var isChest = g.First().f.SourceIsChest;
                sb.Append($"<div class=\"row{(isChest ? "" : " warn")}\"><span>{(isChest ? "" : "⚑ ")}{H(g.Key)}</span><span class=\"n\">{N(g.Count())}</span><div class=\"track\"><i style=\"width:{(100.0 * g.Count() / srcMax).ToString("0.##", CultureInfo.InvariantCulture)}%\"></i></div></div>");
            }
            sb.Append("</div></div></section></div>");

            sb.Append($"""
                <section class="fill"><div class="sh"><h2>{L.T("Tesoros obtenidos", "Treasures obtained")}</h2><span class="meta">{L.Count(treasureRows.Count, "tesoro", "tesoros", "treasure", "treasures")}</span></div>
                <div class="card tw"><table><thead><tr><th>{L.T("Partida", "Match")}</th><th>{L.T("Tesoro", "Treasure")}</th><th>{L.T("Conseguido por", "Obtained by")}</th><th>{L.T("Momento", "Time")}</th><th>{L.T("Origen", "Source")}</th></tr></thead><tbody>
                """);
            foreach (var (m, f) in treasureRows)
            {
                var when = f.ObtainedAtSeconds is float t ? (f.TimeExact ? Pretty.Clock(t) : $"<span class=\"sm\">{L.T("hacia", "around")}</span> {Pretty.Clock(t)}") : "<span class=\"muted\">—</span>";
                var source = $"{(f.SourceIsChest ? "" : "⚑ ")}{H(f.SourceLabel)}";
                // Quién abrió el cofre que dio el tesoro: tú, un compañero (por su nombre) o desconocido (lo recibió el
                // escuadrón, pero el cofre no está en el replay).
                var by = f.Finder == TreasureFind.FinderUnknown ? $"<span class=\"muted\">{H(f.FinderLabel(m))}</span>" : H(f.FinderLabel(m));
                sb.Append($"<tr{(f.SourceIsChest ? "" : " class=\"unusual\"")}><td class=\"nw\">{L.DateTimeShort(When(m))}<div class=\"sm\">{H(Pretty.Playlist(m.Playlist))}</div></td><td>{TreasureCell(f, When(m))}</td><td class=\"nw\">{by}</td><td class=\"nw num\">{when}</td><td>{source}</td></tr>");
            }
            sb.Append("</tbody></table></div></section>");
        }
        else
        {
            sb.Append($"<div class=\"card\"><div class=\"empty big\"><div class=\"star\">★</div>{L.T("Todavía no has encontrado tesoros. Aparecerán aquí con el momento y el cofre de origen.", "No treasures yet. They will show up here with the time and the chest they came from.")}</div></div>");
            AppendTreasureHunt(sb, treasureRows);
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

        sb.Append($"</main></div><script>{TabsScript}</script></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Tabla de partidas (fecha, modo, puesto, cofres, tesoros). Con «Compañeros»: tus cofres, el equipo (≥) y, bajo
    /// el modo, los cofres vistos de cada compañero.
    /// </summary>
    private static void AppendMatchTable(StringBuilder sb, List<MatchRecord> rows, int chestMax, bool withMates, IReadOnlyCollection<string> kinds)
    {
        var hasElims = rows.Any(m => m.Eliminations is > 0);
        var cols = 5 + (hasElims ? 1 : 0) + (withMates ? 1 : 0);
        var chestsHead = withMates ? L.T("Tú", "You") : L.T("Cofres", "Chests");
        sb.Append($"""<div class="card tw"><table class="matches"><thead><tr><th>{L.T("Fecha", "Date")}</th><th>{L.T("Modo", "Mode")}</th><th>{L.T("Puesto", "Placement")}</th>{(hasElims ? $"<th class=\"r\">{L.T("Elim.", "Elims")}</th>" : "")}<th class="r">{chestsHead}</th>{(withMates ? $"<th class=\"r\">{L.T("Equipo", "Team")}</th>" : "")}<th>{L.T("Tesoros", "Treasures")}</th></tr></thead><tbody>""");
        if (rows.Count == 0)
        {
            sb.Append($"<tr><td colspan=\"{cols}\" class=\"empty\">{L.T("Aún no hay partidas analizadas. Juega una y aparecerá aquí.", "No matches analyzed yet. Play one and it will show up here.")}</td></tr>");
        }
        foreach (var m in rows)
        {
            var treasure = m.TreasureFinds.Count == 0 ? "<span class=\"muted\">—</span>"
                : "<div class=\"tlist\">" + string.Concat(m.TreasureFinds.Select(f => $"<span class=\"star\">{(f.Featured ? "★★" : "★")} {H(Prizes.Describe(f, When(m)).Name)}</span>")) + "</div>";
            // Eliminated with no reboot: the squad's treasures after that moment are not in the replay, so the
            // count can be lower than the post-match screen. Said here so that the difference has an explanation.
            if (m.EliminatedAtSeconds is float dead && m.Teammates is { Count: > 0 } && !m.TreasureScreenRead)
            {
                treasure += $"<div class=\"sm\" title=\"{H(L.T("Tras tu eliminación definitiva, los tesoros que consiga el escuadrón no quedan en el replay: pueden faltar respecto a la pantalla de fin de partida.", "After your final elimination, the treasures the squad gets are not in the replay: some may be missing compared with the post-match screen."))}\">⚠ {H(L.F("Eliminado en el {0}: tesoros posteriores no visibles", "Eliminated at {0}: later treasures not visible", Pretty.Clock(dead)))}</div>";
            }
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
                    ? $"<td class=\"r nw\"><span class=\"sm\">≥</span> <b class=\"cy\">{N0(m.OwnChests + m.TeammatesSeen(kinds))}</b></td>"
                    : "<td class=\"r\"><span class=\"muted\">—</span></td>";
            var elims = hasElims ? $"<td class=\"r\">{(m.Eliminations is int e ? N0(e) : "<span class=\"muted\">—</span>")}</td>" : "";
            sb.Append($"<tr{(m.Placement == 1 ? " class=\"won\"" : "")}><td class=\"nw\">{L.DateTimeShort(When(m))}</td><td><span class=\"badge mode\">{H(Pretty.Playlist(m.Playlist))}</span>{team}</td><td><span class=\"badge{placeClass}\">{placeText}</span></td>{elims}<td><div class=\"chests\"><span class=\"mini\"><i style=\"width:{barW}%\"></i></span><b>{own}</b></div></td>{teamCell}<td>{treasure}</td></tr>");
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
    /// Resumen de la caza del tesoro: los premios de esta semana como fichas (los conseguidos, en oro), lo que puede
    /// tocar cualquier semana en una fila y las demás semanas plegadas en «Ver todas las semanas».
    /// </summary>
    private static void AppendTreasureHunt(StringBuilder sb, List<(MatchRecord m, TreasureFind f)> rows)
    {
        var got = rows.Select(r => Prizes.Describe(r.f, When(r.m)).Name)
            .GroupBy(n => n, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var today = DateTime.Now.Date;
        var thisWeek = Prizes.WeekOf(today);
        DateTime WeekEnd(int week) => week < Prizes.WeekStarts.Length ? Prizes.WeekStarts[week] : Prizes.End;
        string Day(DateTime d) => d.ToString(L.En ? "MMM d" : "d MMM", Loc);
        string Range(int week) => $"{Day(Prizes.WeekStarts[week - 1])} – {Day(WeekEnd(week).AddDays(-1))}";
        string Unconfirmed(Prizes.Prize p) => p.Announced ? "" : $"<span class=\"tag\">{L.T("sin confirmar", "unconfirmed")}</span>";
        string Tile(Prizes.Prize p)
        {
            var n = got.GetValueOrDefault(p.Name);
            return $"<div class=\"tile{(n > 0 ? " got" : "")}\"><span class=\"tt\">{H(Prizes.TypeName(p.Type))}</span><b>{H(p.Name)}</b>"
                   + (n > 0 ? $"<span class=\"won\">★ ×{n}</span>" : Unconfirmed(p)) + "</div>";
        }
        string Line(Prizes.Prize p)
        {
            var n = got.GetValueOrDefault(p.Name);
            return $"<li class=\"{(n > 0 ? "got" : "")}\"><span>{H(p.Name)}</span><small>{H(Prizes.TypeName(p.Type))}</small>{(n > 0 ? $"<b>×{n}</b>" : "")}</li>";
        }

        var meta = thisWeek > 0
            ? $"{L.F("Semana {0}", "Week {0}", thisWeek)} · {Range(thisWeek)} · {L.F("quedan {0}", "{0} left", L.Count((WeekEnd(thisWeek) - today).Days, "día", "días", "day", "days"))}"
            : L.T("Evento del 1 de noviembre al 5 de diciembre", "Event from November 1 to December 5");
        sb.Append($"""<section><div class="sh"><h2>{L.T("Caza del tesoro", "Treasure hunt")}</h2><span class="meta">{meta}</span></div><div class="card hunt">""");

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

        // Cualquier semana: acceso a la tienda y los artilugios (estos llegan sin nombre: se cuentan juntos).
        var gizmos = rows.Count(r => !r.f.Featured);
        var chips = Prizes.All.Where(p => p.Week == 0).Select(p =>
        {
            var n = got.GetValueOrDefault(p.Name);
            return $"<span class=\"pchip{(n > 0 ? " got" : "")}\">{H(p.Name)}{(n > 0 ? $" <b>×{n}</b>" : "")}</span>";
        });
        sb.Append($"""<div class="always"><span class="al">{L.T("Todas las semanas", "Every week")}</span>{string.Concat(chips)}"""
                  + (gizmos > 0 ? $"""<span class="pchip got">★ {L.Count(gizmos, "artilugio", "artilugios", "gizmo", "gizmos")}</span>""" : "")
                  + "</div>");

        // Las demás semanas, plegadas.
        sb.Append($"""<details class="weeks"><summary>{L.T("Ver todas las semanas", "See every week")}</summary><div class="wgrid">""");
        for (var w = 1; w <= Prizes.WeekStarts.Length; w++)
        {
            var list = Prizes.All.Where(p => p.Week == w).ToList();
            var body = list.Count > 0 ? $"<ul>{string.Concat(list.Select(Line))}</ul>" : $"<p class=\"sm\">{L.T("Premios aún no anunciados.", "Prizes not announced yet.")}</p>";
            sb.Append($"""<div class="wk{(w == thisWeek ? " now" : "")}"><div class="wh"><b>{L.F("Semana {0}", "Week {0}", w)}</b><span>{Range(w)}</span>{(w == thisWeek ? $"<span class=\"tag gold\">{L.T("Esta semana", "This week")}</span>" : "")}</div>{body}</div>""");
        }
        sb.Append("</div></details></div></section>");
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
              <div class="mini-kpi"><b class="cy"><small>≥</small>{Avg(teamAll, teamMatches.Count)}</b><span>{L.T("del equipo por partida", "per match for the team")}</span></div>
              <div class="mini-kpi"><b class="acc">{Avg(teamMine, teamMatches.Count)}</b><span>{L.T("tuyos por partida", "yours per match")}</span></div>
              <div class="mini-kpi"><b><small>≥</small>{(teamAll > 0 ? (100.0 * teamMates / teamAll).ToString("0", Loc) : "0")} %</b><span>{L.T("aportan tus compañeros", "from your teammates")}</span></div>
            </div></section>
            """);

        // Top de compañeros: clasificaciones de 5 puestos (cofres vistos, media, récord y victorias juntos).
        var medals = new[] { "🥇", "🥈", "🥉" };
        void Board(StringBuilder b, string title, string? foot, List<(string Key, string Name, double Value, string Text, string Sub)> items)
        {
            if (items.Count == 0)
            {
                return;
            }
            var max = Math.Max(0.0001, items.Max(i => i.Value));
            b.Append($"<div class=\"board\"><h3>{title}</h3><ol>");
            for (var i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var rank = i < medals.Length ? $"<span class=\"rk medal\">{medals[i]}</span>" : $"<span class=\"rk\">{i + 1}</span>";
                var w = (100.0 * it.Value / max).ToString("0.##", CultureInfo.InvariantCulture);
                b.Append($"<li{(i == 0 ? " class=\"first\"" : "")}>{rank}<span class=\"av sm-av\" style=\"background:{Tone(it.Key)}\">{Initial(it.Name)}</span>"
                    + $"<span class=\"who2\"><span class=\"nm\" title=\"{H(it.Name)}\">{H(it.Name)}</span><span class=\"sub\">{it.Sub}</span></span><b>{it.Text}</b>"
                    + $"<span class=\"bar\"><i style=\"width:{w}%\"></i></span></li>");
            }
            b.Append("</ol>");
            if (foot != null)
            {
                b.Append($"<div class=\"bfoot\">{foot}</div>");
            }
            b.Append("</div>");
        }
        string Games(int n) => L.Count(n, "partida", "partidas", "match", "matches");
        var minAvg = all.Count(r => r.Matches >= 3) >= 3 ? 3 : 2;
        var boards = new StringBuilder();
        Board(boards, L.T("Más cofres vistos", "Most chests seen"), null, all.OrderByDescending(r => r.Chests).ThenByDescending(r => r.Matches).Take(5)
            .Select(r => (r.Key, r.Name, (double)r.Chests, $"<small>≥</small>{N(r.Chests)}", Games(r.Matches))).ToList());
        Board(boards, L.T("Mejor media por partida", "Best average per match"),
            L.F("Tu media en partidas en equipo: {0}", "Your average in team matches: {0}", Avg(teamMine, teamMatches.Count)),
            all.Where(r => r.Matches >= minAvg).OrderByDescending(r => (double)r.Chests / r.Matches).Take(5)
                .Select(r => (r.Key, r.Name, (double)r.Chests / r.Matches, $"<small>≥</small>{((double)r.Chests / r.Matches).ToString("0.0", Loc)}", Games(r.Matches))).ToList());
        Board(boards, L.T("Récord en una partida", "Best single match"), null, all.Where(r => r.Best > 0).OrderByDescending(r => r.Best).ThenByDescending(r => r.Matches).Take(5)
            .Select(r => (r.Key, r.Name, (double)r.Best, $"<small>≥</small>{N(r.Best)}", L.T("cofres en una partida", "chests in one match"))).ToList());
        Board(boards, L.T("Más victorias juntos", "Most wins together"), null, all.Where(r => r.Wins > 0).OrderByDescending(r => r.Wins).ThenBy(r => r.Matches).Take(5)
            .Select(r => (r.Key, r.Name, (double)r.Wins, $"🏆 {N(r.Wins)}", L.F("{0} de {1}", "{0} of {1}", Pct(r.Wins, r.Matches), Games(r.Matches)))).ToList());
        if (boards.Length > 0)
        {
            sb.Append($"""<section><div class="sh"><h2>{L.T("Top de compañeros", "Top teammates")}</h2><span class="meta">{L.T("cofres vistos: un mínimo", "chests seen: a minimum")}</span></div><div class="boards">{boards}</div></section>""");
        }

        // Abajo, una sola sección con un selector: por partida, con quién más juegas y todos tus compañeros.
        sb.Append("<section class=\"fill segsec\"><div class=\"sh\"><div class=\"seg\" role=\"tablist\"></div><span class=\"meta\"></span></div>");

        // Por partida: tú, cada compañero y el equipo (las 30 más recientes con compañeros).
        var recentTeam = teamMatches.Take(30).ToList();
        if (recentTeam.Count > 0)
        {
            var teamMax = Math.Max(1, recentTeam.Max(m => m.OwnChests + m.TeammatesSeen(extraKinds)));
            sb.Append($"""
                <div class="segp card tw" data-title="{L.T("Por partida", "Per match")}" data-meta="{L.F("las {0} más recientes", "latest {0}", recentTeam.Count)}"><table><thead><tr><th>{L.T("Fecha", "Date")}</th><th>{L.T("Puesto", "Placement")}</th><th class="r">{L.T("Tú", "You")}</th><th>{L.T("Compañeros", "Teammates")}</th><th class="r">{L.T("Equipo", "Team")}</th></tr></thead><tbody>
                """);
            foreach (var m in recentTeam)
            {
                var mates = string.Concat(m.Teammates!.OrderByDescending(t => Seen(t)).Select(t =>
                    $"<span class=\"chip\"><span class=\"av sm-av\" style=\"background:{Tone(t.EpicId ?? t.Name)}\">{Initial(t.Name)}</span><span class=\"cn\">{H(t.Name)}</span> <b>≥ {N(Seen(t))}</b></span>"));
                var total = m.OwnChests + m.TeammatesSeen(extraKinds);
                var placeClass = m.Placement switch { 1 => " win", > 0 and <= 10 => " top", _ => "" };
                var place = m.Placement == 1 ? "🏆 " + H(Pretty.Placement(m)) : H(Pretty.Placement(m));
                var w = (100.0 * total / teamMax).ToString("0.##", CultureInfo.InvariantCulture);
                var mineW = (100.0 * m.OwnChests / teamMax).ToString("0.##", CultureInfo.InvariantCulture);
                sb.Append($"<tr><td class=\"nw\">{L.DateTimeShort(When(m))}<div class=\"sm\">{H(Pretty.Playlist(m.Playlist))}</div></td><td><span class=\"badge{placeClass}\">{place}</span></td>"
                    + $"<td class=\"r\"><b class=\"acc\">{N(m.OwnChests)}</b></td><td><div class=\"mates-line\">{mates}</div></td>"
                    + $"<td><div class=\"teambar\"><span class=\"tb\"><i class=\"me\" style=\"width:{mineW}%\"></i><i style=\"width:{w}%\"></i></span><b class=\"cy\"><small>≥</small>{N(total)}</b></div></td></tr>");
            }
            sb.Append("</tbody></table></div>");
        }

        // Compañeros de relleno con una sola partida: se resumen en una línea en vez de una fila cada uno.
        var rows = all.Where(r => r.Matches >= 2).ToList();
        var oneOff = all.Count - rows.Count;
        if (rows.Count == 0)
        {
            rows = all.Take(20).ToList();
            oneOff = Math.Max(0, all.Count - 20);
        }

        // Los tres con los que más juegas, en tarjetas.

        sb.Append($"""<div class="segp" data-title="{L.T("Con quién más juegas", "Who you play with most")}" data-meta="{L.Count(all.Count, "compañero", "compañeros", "teammate", "teammates")} · {L.Count(tracked, "partida en equipo", "partidas en equipo", "team match", "team matches")}"><div class="mates">""");
        foreach (var r in rows.Take(3))
        {
            var avg = (double)r.Chests / r.Matches;
            sb.Append($"""
                <div class="mate">
                  <div class="who"><div class="av" style="background:{Tone(r.Key)}">{Initial(r.Name)}</div>
                    <div class="grow"><div class="nm" title="{H(r.Name)}">{H(r.Name)}</div><div class="sm">{L.Count(r.Matches, "partida juntos", "partidas juntos", "match together", "matches together")} · {L.F("aporta ≥ {0} del equipo", "brings ≥ {0} of the team", Pct(r.Chests, r.Team))}</div></div></div>
                  <div class="st">
                    <div><b class="acc">{N(r.Wins)}</b><span>{L.T("victorias", "wins")} · {Pct(r.Wins, r.Matches)}</span></div>
                    <div><b><small>≥</small>{avg.ToString("0.0", Loc)}</b><span>{L.T("cofres por partida", "chests per match")}</span></div>
                    <div><b class="cy">{((double)r.Mine / r.Matches).ToString("0.0", Loc)}</b><span>{L.T("tu media juntos", "your average together")}</span></div>
                  </div>
                </div>
                """);
        }
        sb.Append("</div></div>");

        var maxAvg = Math.Max(0.1, rows.Max(r => (double)r.Chests / r.Matches));
        sb.Append($"""
            <div class="segp card tw" data-title="{L.T("Todos tus compañeros", "All your teammates")}" data-meta="{L.T("2 partidas o más", "2 matches or more")}"><table><thead><tr><th>{L.T("Compañero", "Teammate")}</th><th class="r">{L.T("Partidas juntos", "Matches together")}</th><th class="r">{L.T("Victorias", "Wins")}</th><th class="r">{L.T("Cofres vistos", "Chests seen")}</th><th class="r">{L.T("Media", "Average")}</th><th class="r">{L.T("Récord", "Best")}</th><th class="r">{L.T("Aporta", "Share")}</th><th class="r">{L.T("Tu media", "Your average")}</th><th>{L.T("Última partida", "Last match")}</th></tr></thead><tbody>
            """);
        foreach (var r in rows)
        {
            var avg = (double)r.Chests / r.Matches;
            var barW = (100.0 * avg / maxAvg).ToString("0.##", CultureInfo.InvariantCulture);
            var wins = r.Wins > 0 ? $"<span class=\"badge win\">🏆 {N(r.Wins)}</span> <span class=\"sm\">{Pct(r.Wins, r.Matches)}</span>" : "<span class=\"muted\">—</span>";
            sb.Append($"<tr><td><div class=\"who\"><span class=\"av sm-av\" style=\"background:{Tone(r.Key)}\">{Initial(r.Name)}</span><span class=\"nm\">{H(r.Name)}</span></div></td><td class=\"r\">{N(r.Matches)}</td><td class=\"r nw\">{wins}</td><td class=\"r nw\"><span class=\"sm\">≥</span> <b>{N(r.Chests)}</b></td>"
                + $"<td><div class=\"chests\"><span class=\"mini\"><i style=\"width:{barW}%\"></i></span><b>{avg.ToString("0.0", Loc)}</b></div></td>"
                + $"<td class=\"r\">{N(r.Best)}</td><td class=\"r nw\"><span class=\"sm\">≥</span> {Pct(r.Chests, r.Team)}</td><td class=\"r\">{((double)r.Mine / r.Matches).ToString("0.0", Loc)}</td><td class=\"nw sm\">{L.DateTimeShort(r.Last)}</td></tr>");
        }
        if (oneOff > 0)
        {
            var more = L.Count(oneOff, "compañero", "compañeros", "teammate", "teammates");
            sb.Append($"<tr><td colspan=\"9\" class=\"sm foot\">{(all.Count(r => r.Matches >= 2) > 0
                ? L.F("Y {0} con una sola partida (relleno de equipo), no mostrados.", "And {0} with a single match (team fill), not shown.", more)
                : L.F("Y {0} más.", "And {0} more.", more))}</td></tr>");
        }
        sb.Append("</tbody></table></div></section>");
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
        return $"""<div class="profiles">{L.T("Otros perfiles en este PC: ", "Other profiles on this PC: ")}{string.Join(", ", others)} · {L.T("cambia de perfil en Ajustes", "switch profile in Settings")}</div>""";
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
