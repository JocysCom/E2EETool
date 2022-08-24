using System;
using System.Collections.Generic;
using System.Drawing;
using Accessibility;

namespace MSAA
{
	public class MsaaItem
	{
		public MsaaItem(IAccessible acc, int childId = 0)
		{
			_Accessible = acc;
			_ChildId = childId;
			Refresh();
		}

		public IAccessible Accessible => _Accessible;
		IAccessible _Accessible;

		public int ChildId => _ChildId;
		int _ChildId;


		public string Name => _Name;
		string _Name;

		public string Value => _Value;
		string _Value;

		public MsaaState State => _State;
		MsaaState _State;

		public MsaaRole Role => _Role;
		MsaaRole _Role;

		public bool IsEnabled => !State.HasFlag(MsaaState.Unavailable);
		public bool IsVisible => !State.HasFlag(MsaaState.Invisible);
		public bool IsExist => Handle != IntPtr.Zero;

		public Rectangle Location => _Location;
		Rectangle _Location;

		public IntPtr Handle => _Handle;
		IntPtr _Handle;

		public string DefaultAction => _DefaultAction;
		string _DefaultAction;

		public bool IsValid => _IsValid;
		bool _IsValid;

		public Exception Error => _Error;
		Exception _Error;

		// Depending on role some properties are not available.
		// Use lists to cash these roles and crash less.
		public static List<MsaaRole> NoDefaultAction = new List<MsaaRole>();
		public static List<MsaaRole> NoRole = new List<MsaaRole>();
		public static List<MsaaRole> NoName = new List<MsaaRole>();
		public static List<MsaaRole> NoValue = new List<MsaaRole>();
		public static List<MsaaRole> NoState = new List<MsaaRole>();

		public void Refresh()
		{
			_IsValid = false;
			_Name = string.Empty;
			_Value = string.Empty;
			_Role = default(MsaaRole);
			_State = default(MsaaState);
			_Handle = IntPtr.Zero;
			_Location = new Rectangle();
			_DefaultAction = string.Empty;
			if (_Accessible == null || _Accessible == default(IAccessible))
				return;
			if (ChildId > 0)
				_Accessible = _Accessible.accChild[ChildId] as IAccessible;
			if (_Accessible != null)
				_Handle = Msaa.WindowFromAccessibleObject(_Accessible);
			TrySetValue(() => _Name = _Accessible.accName[_ChildId], NoName);
			TrySetValue(() => _Value = _Accessible.accValue[_ChildId], NoValue);
			TrySetValue(() => _Role = (MsaaRole)Convert.ToUInt32(_Accessible.accRole[_ChildId]), NoRole);
			TrySetValue(() => _State = (MsaaState)Convert.ToUInt32(_Accessible.accState[_ChildId]), NoState);
			TrySetValue(() => _DefaultAction = _Accessible.accDefaultAction[_ChildId], NoDefaultAction);
			SetLocation(_Accessible);
			_IsValid = true;
		}

		public void TrySetValue(Action action, List<MsaaRole> excludeList)
		{
			try
			{
				if (!excludeList.Contains(Role))
					action.Invoke();
			}
			catch (Exception ex)
			{
				// DISP_E_MEMBERNOTFOUND
				if ((uint)ex.HResult == 0x80020003)
				{
					lock (excludeList)
						if (!excludeList.Contains(Role))
							excludeList.Add(Role);
				}
				else
				{
					throw;
				}
			}
		}

		private void SetLocation(IAccessible acc)
		{
			if (acc == null)
				return;
			int x;
			int y;
			int width;
			int hieght;
			acc.accLocation(out x, out y, out width, out hieght, 0);
			_Location = new Rectangle(x, y, x + width, y + hieght);
		}

		override public string ToString()
		{
			return $"{Role} '{Name}': {Value}";
		}

		public MsaaItem Parent =>
			Msaa.GetAccessibleParent(this);

		public MsaaItem[] Children =>
			Msaa.GetAccessibleChildren(this);

	}
}
