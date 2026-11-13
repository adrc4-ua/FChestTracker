using System.Drawing;
using System.Runtime.Versioning;

namespace FCT.App;

/// <summary>The icon of FCT.exe itself, for the windows' title bars (null lets Windows use its generic one).</summary>
[SupportedOSPlatform("windows")]
internal static class AppIcon
{
    public static Icon? Load()
    {
        if (Environment.ProcessPath is not { } exe)
        {
            return null;
        }
        try
        {
            return Icon.ExtractAssociatedIcon(exe);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
