using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Serialization;
using Accessibility;

namespace MSAA
{

	public class MsaaItem
	{
		public MsaaItem()
		{
		}

		public void Load(IAccessible acc, int childId = 0)
		{
			_Accessible = acc;
			_ChildId = childId;
			Refresh();
		}

		/// <summary>
		/// Manually set ID. Used to find this object by Id in XML.
		/// </summary>
		[XmlAttribute, DefaultValue(0)]
		public int Id { get; set; }

		[XmlAttribute]
		public MsaaRole Role { get => _Role; set => _Role = value; }
		MsaaRole _Role;

		[XmlAttribute, DefaultValue(typeof(MsaaState), nameof(MsaaState.Normal))]
		public MsaaState State { get => _State; set => _State = value; }
		MsaaState _State;

		public IAccessible Accessible { get => _Accessible; set => _Accessible = value; }
		IAccessible _Accessible;

		[XmlAttribute, DefaultValue(0)]
		public int ChildId { get => _ChildId; set => _ChildId = value; }
		int _ChildId;


		[XmlAttribute, DefaultValue("")]
		public string Name { get => _Name; set => _Name = value; }
		string _Name;

		[XmlAttribute, DefaultValue("")]
		public string Value { get => _Value; set => _Value = value; }
		string _Value;

		public bool IsEnabled => !State.HasFlag(MsaaState.Unavailable);
		public bool IsVisible => !State.HasFlag(MsaaState.Invisible);
		public bool IsExist => Handle != 0;

		public Rectangle Location => _Location;
		Rectangle _Location;

		[XmlAttribute]
		public int Handle { get => _Handle; set => _Handle = value; }
		int _Handle;

		public string DefaultAction => _DefaultAction;
		string _DefaultAction;

		public bool IsValid => _IsValid;
		bool _IsValid;

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
			_Handle = 0;
			_Location = new Rectangle();
			_DefaultAction = string.Empty;
			if (_Accessible == null || _Accessible == default(IAccessible))
				return;
			if (ChildId > 0)
			{
				var accChild = _Accessible.accChild[ChildId] as IAccessible;
				if (accChild != null)
				{
					_Accessible = accChild;
				}
			}
			if (_Accessible != null)
				_Handle = Msaa.WindowFromAccessibleObject(_Accessible).ToInt32();


			TrySetValue(() => _Name = _Accessible.accName[_ChildId], NoName);
			TrySetValue(() => _Value = _Accessible.accValue[_ChildId], NoValue);
			TrySetValue(() => _Role = (MsaaRole)Convert.ToUInt32(_Accessible.accRole[_ChildId]), NoRole);
			TrySetValue(() => _State = (MsaaState)Convert.ToUInt32(_Accessible.accState[_ChildId]), NoState);
			TrySetValue(() => _DefaultAction = _Accessible.accDefaultAction[_ChildId], NoDefaultAction);
			SetLocation(_Accessible);
			_IsValid = true;
		}

		public void SetValue(string value) =>
			_Accessible.accValue[_ChildId] = value;

		public void SetName(string value) =>
			_Accessible.accName[_ChildId] = value;

		//https://docs.microsoft.com/en-us/windows/win32/winauto/selflag
		public const int SELFLAG_NONE = 0;
		public const int SELFLAG_TAKEFOCUS = 0x1;

		public void Select() =>
			_Accessible.accSelect(SELFLAG_NONE, _ChildId);

		public void Focus() =>
			_Accessible.accSelect(SELFLAG_TAKEFOCUS, _ChildId);

		private void TrySetValue(Action action, List<MsaaRole> excludeList)
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

		[XmlElement(nameof(MsaaItem))]
		public MsaaItem[] Children
		{
			get { return _Children = _Children ?? Msaa.GetAccessibleChildren(this); }
			set { _Children = value; }
		}
		MsaaItem[] _Children;

	}



}
