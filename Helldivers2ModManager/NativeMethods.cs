using System.Runtime.InteropServices;

namespace Helldivers2ModManager;

internal static partial class NativeMethods
{
	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool SetForegroundWindow(nint hWnd);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

	internal const int SW_RESTORE = 9;
}
