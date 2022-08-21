using System;
using System.Collections.Generic;
using System.Drawing;
using Accessibility;

namespace MSAA
{
	public class MsaaAccessible
	{
		public MsaaAccessible(IAccessible acc, int childId = 0)
		{
			_Accessible = acc;
			_ChildId = childId;
			SetAccessible(acc);
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

		public void Refresh()
		{
			SetAccessible(_Accessible);
		}

		public bool IsValid => _IsValid;
		bool _IsValid;

		public Exception Error => _Error;
		Exception _Error;


		public static List<MsaaRole> NoDefaultAction = new List<MsaaRole>();
		public static List<MsaaRole> NoValue = new List<MsaaRole>();

		private void SetAccessible(IAccessible acc)
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
			try
			{
				_Role = (MsaaRole)Convert.ToUInt32(acc.accRole[_ChildId]);
			}
			catch (Exception ex)
			{
				_Error = ex;
				return;
			}
			if (ChildId > 0)
				_Accessible = acc.accChild[ChildId] as IAccessible;
			if (acc != null)
				_Handle = Msaa.WindowFromAccessibleObject(acc);
			try
			{
				_Name = acc.accName[_ChildId];
			}
			catch (Exception ex)
			{
				_Error = ex;
			}
			try
			{
				if (!NoValue.Contains(Role))
					_Value = acc.accValue[_ChildId];
			}
			catch (Exception ex)
			{
				// DISP_E_MEMBERNOTFOUND
				if ((uint)ex.HResult == 0x80020003)
				{
					lock (NoValue)
						if (!NoValue.Contains(Role))
							NoValue.Add(Role);
				}
				else
				{
					throw;
				}
			}
			_State = (MsaaState)Convert.ToUInt32(acc.accState[_ChildId]);
			try
			{
				if (!NoDefaultAction.Contains(Role))
					_DefaultAction = acc.accDefaultAction[_ChildId];
			}
			catch (Exception daEx)
			{
				// DISP_E_MEMBERNOTFOUND
				if ((uint)daEx.HResult == 0x80020003)
				{
					lock (NoDefaultAction)
						if (!NoDefaultAction.Contains(Role))
							NoDefaultAction.Add(Role);
				}
				else
				{
					throw;
				}
			}
			SetLocation(acc);
			_IsValid = true;
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

	}
}
