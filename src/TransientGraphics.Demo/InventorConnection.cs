using System.Runtime.InteropServices;

namespace TransientGraphics.Demo;

// Attaches to the Inventor session that is already running, from outside its process.
// Inventor registers itself in the Running Object Table under the ProgID "Inventor.Application";
// GetActiveObject looks it up. Marshal.GetActiveObject is not available on .NET 8, hence the
// two P/Invokes.
public static class InventorConnection
{
    [DllImport("ole32.dll")]
    private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string progId, out Guid clsid);

    [DllImport("oleaut32.dll")]
    private static extern int GetActiveObject(ref Guid clsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object obj);

    public static Inventor.Application GetRunning()
    {
        Marshal.ThrowExceptionForHR(CLSIDFromProgID("Inventor.Application", out var clsid));

        if (GetActiveObject(ref clsid, IntPtr.Zero, out var obj) != 0)
            throw new InvalidOperationException("No running Inventor session found. Start Inventor first.");

        return (Inventor.Application)obj;
    }
}
