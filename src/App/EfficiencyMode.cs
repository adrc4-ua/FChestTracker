using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FCT.App;

/// <summary>
/// Asks Windows to treat this process as background work ("efficiency mode" on Windows 11, power throttling since
/// Windows 10 1709): slower, low-power cores, so the game always comes first.
/// </summary>
internal static class EfficiencyMode
{
    public static void Enable()
    {
        var throttle = new PowerThrottlingState { Version = 1, ControlMask = ThrottleExecutionSpeed, StateMask = ThrottleExecutionSpeed };
        using var self = Process.GetCurrentProcess();
        SetProcessInformation(self.Handle, ProcessPowerThrottling, ref throttle, (uint)Marshal.SizeOf<PowerThrottlingState>());
    }

    // PROCESS_INFORMATION_CLASS.ProcessPowerThrottling and PROCESS_POWER_THROTTLING_EXECUTION_SPEED (processthreadsapi.h).
    private const int ProcessPowerThrottling = 4;
    private const uint ThrottleExecutionSpeed = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct PowerThrottlingState
    {
        public uint Version, ControlMask, StateMask;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessInformation(IntPtr process, int informationClass, ref PowerThrottlingState information, uint size);
}
