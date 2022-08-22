using System;
using Accessibility;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Drawing;
using System.Security;

namespace MSAA
{
	public static class Msaa
	{

		public static MsaaItem[] GetAccessibleChildren(MsaaItem item, int childStart = 0)
		{
			if (item.ChildId != 0)
				return new MsaaItem[0];
			int childCount = item.Accessible.accChildCount;
			// Pointer to an array of VARIANT structures that receives information about the container's children.
			// If the vt member of an array element is VT_I4, then the lVal member for that element is the child ID.
			// If the vt member of an array element is VT_DISPATCH, then the pdispVal member for that element is the address of the child object's IDispatch interface.
			var children = new object[childCount * 2];
			int childrenCount;
			var result = MsaaWin32.AccessibleChildren(item.Accessible, childStart, children.Length, children, out childrenCount);
			if (result != 0 && result != 1)
				return new MsaaItem[0];
			if (childrenCount == 1 && children[0] is int && (int)children[0] < 0)
				return new MsaaItem[0];
			var items = new MsaaItem[childrenCount];
			for (int i = 0; i < childrenCount; i++)
				items[i] = ObjectToMsaaItem(item.Accessible, children[i]);
			return items;
		}

		public static MsaaItem GetAccessibleParent(MsaaItem item)
		{
			if (item.ChildId != 0)
				return new MsaaItem(item.Accessible, 0);
			var p = (IAccessible)item.Accessible.accParent;
			if (p == null)
				return null;
			return new MsaaItem(p, 0);
		}

		public static MsaaItem ObjectToMsaaItem(IAccessible acc, object o)
		{
			return (o is int childId)
				? new MsaaItem(acc, childId)
				: new MsaaItem((IAccessible)o);
		}

		public static List<IAccessible> GetWindows(Regex windowName = null, params MsaaRole[] roles)
		{
			var handles = GetWindowHandles();
			var list = new List<IAccessible>();
			foreach (var handle in handles)
			{
				var accItem = AccessibleObjectFromWindow(handle);
				// If failed to retrieve the object then continue.
				if (accItem == default(IAccessible))
					continue;
				var name = accItem.accName ?? "";
				var role = (MsaaRole)Convert.ToUInt32(accItem.accRole);
				if (roles.Length > 0 && !roles.Contains(role))
					continue;
				// If filter specified but name don't match then continue.
				if (windowName != null && !windowName.Match(name).Success)
					continue;
				list.Add(accItem);
			}
			return list;
		}

		#region Win32

		/// <summary>
		/// Get the accessible object of the top application window. 
		/// </summary>
		public static IAccessible FindWindow(string className, string caption)
		{
			var handle = MsaaWin32.FindWindow(className, caption);
			return AccessibleObjectFromWindow(handle);
		}

		public static List<IntPtr> GetWindowHandles()
		{
			var list = new List<IntPtr>();
			var lpEnumFnct = new MsaaWin32.EnumWindowsProc((IntPtr hWnd, IntPtr lParam) =>
			{
				list.Add(hWnd);
				return true;
			});
			MsaaWin32.EnumWindows(lpEnumFnct, IntPtr.Zero);
			return list;
		}

		public static IAccessible AccessibleObjectFromWindow(IntPtr hwnd, MsaaCategory category = MsaaCategory.Window)
		{
			var accObject = new object();
			var accItem = default(IAccessible);
			var guidAccessible = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");
			if (hwnd == IntPtr.Zero)
				return accItem;
			MsaaWin32.AccessibleObjectFromWindow(hwnd, (uint)category, ref guidAccessible, ref accObject);
			accItem = (IAccessible)accObject;
			return accItem;
		}

		public static IAccessible AccessibleObjectFromMouseCursor() =>
			AccessibleObjectFromWindow(IntPtr.Zero, MsaaCategory.Cursor);

		public static IAccessible AccessibleObjectFromTextCaret() =>
			AccessibleObjectFromWindow(IntPtr.Zero, MsaaCategory.Caret);

		public static MsaaItem AccessibleObjectFromPoint(int x, int y)
		{
			IAccessible acc;
			object child;
			var point = new Point(x, y);
			var result = MsaaWin32.AccessibleObjectFromPoint(point, out acc, out child);
			if (result != IntPtr.Zero)
			{
				var ex = new Win32Exception();
				throw new Exception(ex.Message, ex);
			}
			return ObjectToMsaaItem(acc, child);
		}

		public static IntPtr WindowFromAccessibleObject(IAccessible _accessible)
		{
			var hwnd = IntPtr.Zero;
			MsaaWin32.WindowFromAccessibleObject(_accessible, ref hwnd);
			return hwnd;
		}

		public static MsaaItem[] GetSelectedMsaaItems(MsaaItem item)
		{
			var items = Array.Empty<MsaaItem>();
			if (item.ChildId != 0)
				return items;
			object selection;
			try
			{
				selection = item.Accessible.accSelection;
			}
			catch (NotImplementedException)
			{
				return items;
			}
			catch (COMException)
			{
				return items;
			}
			if (selection == null)
				return new MsaaItem[0];
			if (selection is IEnumVariant)
			{
				var e = (IEnumVariant)selection;
				e.Reset();
				var retval = new List<MsaaItem>();
				var tmp = new object[1];
				while (e.Next(1, tmp, IntPtr.Zero) == 0)
				{
					if (tmp[0] is int && (int)tmp[0] < 0)
						break;
					retval.Add(ObjectToMsaaItem(item.Accessible, tmp[0]));
				}
				return retval.ToArray();
			}
			else
			{
				if (selection is int && (int)selection < 0)
					return items;
				return new MsaaItem[] { ObjectToMsaaItem(item.Accessible, selection) };
			}
		}

		/*
		#region Get HTML object

		[ComImport(), ComVisible(true), Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IServiceProvider
		{
			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
		}

		private mshtml.IHTMLElement ToHtmlElement(IAccessible acc)
		{
			if (acc == null)
				return null;
			var provider = acc as IServiceProvider;
			if (provider == null)
				return null;
			object htmlElement;
			Guid guid = typeof(IHTMLElement).GUID;
			int hRes = provider.QueryService(ref guid, ref guid, out htmlElement);
			if (hRes != 0)
				return null;
			return htmlElement as mshtml.IHTMLElement;
		}

		#endregion
		*/

		#endregion

		#region Interfaces

		[ComImport(), ComVisible(true), Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IServiceProvider
		{
			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
		}

		/// <SecurityNote>
		/// Critical:Elevates to Unmanaged code permission
		/// </SecurityNote>
		[SuppressUnmanagedCodeSecurity]
		[ComImport(), Guid("00020404-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IEnumVariant
		{
			/// <SecurityNote>
			///    Critical: This code elevates to call unmanaged code
			/// </SecurityNote>
			[SecurityCritical, SuppressUnmanagedCodeSecurity]
			[PreserveSig]
			int Next(
				[In, MarshalAs(UnmanagedType.U4)] int celt,
				[In, Out] object rgvar,
				[Out] IntPtr pceltFetched
			);

			void Skip([In, MarshalAs(UnmanagedType.U4)] int celt);

			/// <SecurityNote>
			///    Critical: This code elevates to call unmanaged code
			/// </SecurityNote>
			[SecurityCritical, SuppressUnmanagedCodeSecurity]
			void Reset();

			void Clone([Out, MarshalAs(UnmanagedType.LPArray)] IEnumVariant[] ppenum);
		}

		#endregion

	}
}
