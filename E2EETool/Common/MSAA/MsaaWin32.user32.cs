using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MSAA
{
	/// <summary>
	/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/
	/// </summary>
	public static partial class MsaaWin32
	{
		/// <summary>
		/// An application-defined callback function to receive window handles.
		/// </summary>
		internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern IntPtr FindWindowByClass(string lpClassName, IntPtr zero);

		[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern IntPtr FindWindowByCaption(IntPtr zero, string lpWindowName);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern int GetWindowTextLength(IntPtr hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool BringWindowToTop(IntPtr hwnd);

		/// <summary>
		/// Enumerates all top-level windows on the screen.
		/// </summary>
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		/// <summary>
		/// Enumerates the child windows that belong to the specified parent window.
		/// </summary>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, int lParam);

	}
}
