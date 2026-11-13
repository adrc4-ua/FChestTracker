using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;

namespace FCT.PostMatch;

/// <summary>
/// One frame of one window, with Windows.Graphics.Capture (the API OBS uses for "window capture"): it reads what the
/// window draws even when other windows cover it, in a window or full screen, without touching the game process.
/// The frame stays in memory (never written to disk). A minimized window draws nothing, so there is nothing to read.
/// </summary>
public static class WindowCapture
{
    /// <summary>BGRA pixels of the window (width × height × 4), or null when it cannot be captured.</summary>
    public sealed record Frame(int Width, int Height, byte[] Bgra);

    public static async Task<Frame?> CaptureAsync(IntPtr hwnd, TimeSpan timeout)
    {
        if (hwnd == IntPtr.Zero || IsIconic(hwnd) || !GraphicsCaptureSession.IsSupported())
        {
            return null;
        }

        var item = CreateItemForWindow(hwnd);
        if (item == null || item.Size.Width <= 0 || item.Size.Height <= 0)
        {
            return null;
        }
        using var device = CreateDevice();
        using var pool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, item.Size);
        using var session = pool.CreateCaptureSession(item);
        session.IsCursorCaptureEnabled = false;
        TrySetBorderless(session);

        var done = new TaskCompletionSource<Frame?>(TaskCreationOptions.RunContinuationsAsynchronously);
        pool.FrameArrived += async (p, _) =>
        {
            try
            {
                using var frame = p.TryGetNextFrame();
                if (frame == null || done.Task.IsCompleted)
                {
                    return;
                }
                using var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Ignore);
                done.TrySetResult(ToFrame(bitmap));
            }
            catch (Exception ex)
            {
                done.TrySetException(ex);
            }
        };
        session.StartCapture();
        var finished = await Task.WhenAny(done.Task, Task.Delay(timeout));
        return finished == done.Task ? await done.Task : null;
    }

    private static Frame ToFrame(SoftwareBitmap bitmap)
    {
        using var converted = bitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8 ? null : SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8);
        var source = converted ?? bitmap;
        var bytes = new byte[source.PixelWidth * source.PixelHeight * 4];
        var buffer = new Windows.Storage.Streams.Buffer((uint)bytes.Length);
        source.CopyToBuffer(buffer);
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
        {
            reader.ReadBytes(bytes);
        }
        return new Frame(source.PixelWidth, source.PixelHeight, bytes);
    }

    /// <summary>Windows 11 draws a yellow border around a captured window unless the app asks for none.</summary>
    private static void TrySetBorderless(GraphicsCaptureSession session)
    {
        try
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsBorderRequired"))
            {
                session.IsBorderRequired = false;
            }
        }
        catch
        {
            // Not allowed on this system: the border shows for a fraction of a second.
        }
    }

    // ---- Interop: capture item from a window handle, Direct3D device for the frame pool --------------------------

    [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
        IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
    }

    private static GraphicsCaptureItem? CreateItemForWindow(IntPtr hwnd)
    {
        var factory = WinRT.ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
        var iid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760"); // IGraphicsCaptureItem
        var pointer = interop.CreateForWindow(hwnd, ref iid);
        if (pointer == IntPtr.Zero)
        {
            return null;
        }
        try
        {
            return GraphicsCaptureItem.FromAbi(pointer);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    private static IDirect3DDevice CreateDevice()
    {
        const int hardware = 1, bgraSupport = 0x20, sdkVersion = 7;
        Marshal.ThrowExceptionForHR(D3D11CreateDevice(IntPtr.Zero, hardware, IntPtr.Zero, bgraSupport, IntPtr.Zero, 0, sdkVersion,
            out var d3dDevice, out _, out var context));
        try
        {
            var dxgiIid = new Guid("54EC77FA-1377-44E6-8C32-88FD5F44C84C"); // IDXGIDevice
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(d3dDevice, in dxgiIid, out var dxgiDevice));
            try
            {
                Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out var inspectable));
                try
                {
                    return WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
                }
                finally
                {
                    Marshal.Release(inspectable);
                }
            }
            finally
            {
                Marshal.Release(dxgiDevice);
            }
        }
        finally
        {
            Marshal.Release(context);
            Marshal.Release(d3dDevice);
        }
    }

    [DllImport("d3d11.dll")]
    private static extern int D3D11CreateDevice(IntPtr adapter, int driverType, IntPtr software, uint flags, IntPtr featureLevels,
        uint featureLevelCount, uint sdkVersion, out IntPtr device, out int featureLevel, out IntPtr immediateContext);

    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hwnd);
}
