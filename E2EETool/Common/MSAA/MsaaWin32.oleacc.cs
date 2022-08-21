using System;
using System.Runtime.InteropServices;
using System.Text;
using Accessibility;

namespace MSAA
{
	/// <summary>
	/// https://docs.microsoft.com/en-us/windows/win32/api/oleacc/
	/// </summary>
	public static partial class MsaaWin32
	{

		/// <summary>
		/// Retrieves a localized string that describes an object's state for a single predefined state bit flag.
		/// Because state values are a combination of one or more bit flags, clients call this function more than once to retrieve all state strings.
		/// </summary>
		[DllImport("oleacc.dll")]
		internal static extern uint GetStateText(uint dwStateBit, [Out] StringBuilder lpszStateBit, uint cchStateBitMax);

		/// <summary>
		/// Retrieves the localized string that describes the object's role for the specified role value.
		/// </summary>
		[DllImport("oleacc.dll")]
		internal static extern uint GetRoleText(uint lRole, [Out] StringBuilder lpszRole, uint cchRoleMax);

		[DllImport("oleacc.dll")]
		internal static extern uint WindowFromAccessibleObject(IAccessible pacc, ref IntPtr phwnd);

		/// <summary>
		/// Retrieves the window handle that corresponds to a particular instance of an IAccessible interface.
		/// </summary>
		[DllImport("oleacc.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		internal static extern object AccessibleObjectFromWindow(int hwnd, int dwId, ref Guid riid);

		/// <summary>
		/// Retrieves the address of the specified interface for the object associated with the specified window.
		/// </summary>
		[DllImport("oleacc.dll")]
		internal static extern int AccessibleObjectFromWindow(
			 IntPtr hwnd, uint id, ref Guid iid,
			 [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

		[DllImport("oleacc.dll")]
		public static extern uint AccessibleChildren(
			IAccessible paccContainer, int iChildStart, int cChildren,
			[Out] object[] rgvarChildren, out int pcObtained);

	}
}
