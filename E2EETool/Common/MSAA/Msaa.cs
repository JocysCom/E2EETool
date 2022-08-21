using System;
using Accessibility;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices;

namespace MSAA
{
	public static class Msaa
	{

		public static MsaaAccessible[] GetAccessibleChildren(MsaaAccessible parent, int childStart = 0)
		{
			if (parent.Accessible == null)
				return new MsaaAccessible[0];
			int childCount = 0;
			try
			{
				childCount = parent.Accessible.accChildCount;
			}
			catch { }
			// Pointer to an array of VARIANT structures that receives information about the container's children.
			// If the vt member of an array element is VT_I4, then the lVal member for that element is the child ID.
			// If the vt member of an array element is VT_DISPATCH, then the pdispVal member for that element is the address of the child object's IDispatch interface.
			var children = new object[childCount];
			int count;
			if (childCount != 0)
				MsaaWin32.AccessibleChildren(parent.Accessible, childStart, childCount, children, out count);
			var ias = new List<MsaaAccessible>();
			foreach (var child in children)
			{
				// If element is VT_I4 then...
				if (child is int childId)
				{
					var ms = new MsaaAccessible(parent.Accessible, childId);
					ias.Add(ms);
				}
				else
				{
					ias.Add(new MsaaAccessible((IAccessible)child));
				}
			}
			return ias.ToArray();
		}

		[ComImport(), ComVisible(true), Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),
	   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IServiceProvider
		{
			[return: MarshalAs(UnmanagedType.I4)]
			[PreserveSig]
			int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
		}

		public static MsaaAccessible GetObjectByName(MsaaAccessible parent, Regex name, bool ignoreInvisible)
		{
			MsaaAccessible result = null;
			if (parent == null)
				return null;
			var children = GetAccessibleChildren(parent);
			foreach (var child in children)
			{
				string childName = null;
				var childState = default(MsaaState);
				childName = child.Name;
				childState = (MsaaState)Convert.ToUInt32(child.State);
				if (ignoreInvisible)
				{
					if (childName != null && name.Match(childName).Success && !childState.HasFlag(MsaaState.Invisible))
						return child;
				}
				else
				{
					if (childName != null && name.Match(childName).Success)
						return child;
				}
				if (ignoreInvisible)
				{
					if (!childState.HasFlag(MsaaState.Invisible))
					{
						result = GetObjectByName(child, name, ignoreInvisible);
						if (result != default(IAccessible))
							return result;
					}
				}
				else
				{
					result = GetObjectByName(child, name, ignoreInvisible);
					if (result != default(IAccessible))
						return result;
				}

			}
			return result;
		}

		public static MsaaAccessible GetObjectByNameAndRole(MsaaAccessible parent, Regex name, MsaaRole role, bool ignoreInvisible)
		{
			MsaaAccessible result = null;
			if (parent != null)
			{
				var children = GetAccessibleChildren(parent);
				foreach (var child in children)
				{
					if (ignoreInvisible)
					{
						if (!string.IsNullOrEmpty(child.Name) && name.Match(child.Name).Success && child.Role == role && !child.State.HasFlag(MsaaState.Invisible))
							return child;
					}
					else
					{
						if (!string.IsNullOrEmpty(child.Name) && name.Match(child.Name).Success && child.Role == role)
							return child;
					}

					if (ignoreInvisible)
					{
						if (!child.State.HasFlag(MsaaState.Invisible))
						{
							result = GetObjectByNameAndRole(child, name, role, ignoreInvisible);
							if (result != default(IAccessible))
								return result;
						}
					}
					else
					{
						result = GetObjectByNameAndRole(child, name, role, ignoreInvisible);
						if (result != default(IAccessible))
							return result;
					}
				}
			}
			return result;
		}

		public static void GetAccessibleObjectListByRole(
			MsaaAccessible parent,
			MsaaRole roleText,
			ref List<MsaaAccessible> list,
			bool ignoreInvisible)
		{
			if (parent != null)
			{
				var children = GetAccessibleChildren(parent);
				foreach (var child in children)
				{
					if (ignoreInvisible)
					{
						if (roleText == child.Role && !child.State.HasFlag(MsaaState.Invisible))
							list.Add(child);
					}
					else
					{
						if (roleText == child.Role)
							list.Add(child);
					}

					if (ignoreInvisible)
					{
						if (!child.State.HasFlag(MsaaState.Invisible))
							GetAccessibleObjectListByRole(child, roleText, ref list, ignoreInvisible);
					}
					else
					{
						GetAccessibleObjectListByRole(child, roleText, ref list, ignoreInvisible);
					}
				}
			}
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

		public static IAccessible AccessibleObjectFromWindow(IntPtr hwnd)
		{
			var accObject = new object();
			var accItem = default(IAccessible);
			var guidAccessible = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");
			if (hwnd == IntPtr.Zero)
				return accItem;
			MsaaWin32.AccessibleObjectFromWindow(hwnd, 0, ref guidAccessible, ref accObject);
			accItem = (IAccessible)accObject;
			return accItem;
		}

		public static IntPtr WindowFromAccessibleObject(IAccessible _accessible)
		{
			var hwnd = IntPtr.Zero;
			MsaaWin32.WindowFromAccessibleObject(_accessible, ref hwnd);
			return hwnd;
		}

		#endregion

	}
}
