using System;

namespace MSAA
{

	/// <summary>
	/// Source: C:\Program Files (x86)\Windows Kits\10\Include\10.0.22000.0\um\oleacc.h
	/// </summary>
	[Flags]
	public enum MsaaState: uint
	{
		AlertHigh = 0x10000000,
		AlertLow = 0x04000000,
		AlertMedium = 0x08000000,
		Animated = 0x00004000,
		Busy = 0x00000800,
		Checked = 0x00000010,
		Collapsed = 0x00000400,
		Default = 0x00000100,
		Expanded = 0x00000200,
		ExtSelectable = 0x02000000,
		Floating = 0x00001000,
		Focusable = 0x00100000,
		Focused = 0x00000004,
		HasPopup = 0x40000000,
		HotTracked = 0x00000080,
		Invisible = 0x00008000,
		Linked = 0x00400000,
		Marqueed = 0x00002000,
		Mixed = 0x00000020,
		Moveable = 0x00040000,
		MultiSelectable = 0x01000000,
		Normal = 0x00000000,
		Offscreen = 0x00010000,
		Pressed = 0x00000008,
		Readonly = 0x00000040,
		Selectable = 0x00200000,
		Selected = 0x00000002,
		SelfVoicing = 0x00080000,
		Sizeable = 0x00020000,
		Traversed = 0x00800000,
		Unavailable = 0x00000001,
		Valid = 0x1fffffff,
	}
}