using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;

namespace JocysCom.ClassLibrary.Processes
{
	public static class KeyboardHelper
	{

		#region Keyboard Event

		[DllImport("user32.dll", SetLastError = true)]
		private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

		const int KEYEVENTF_EXTENDEDKEY = 1; // Key Down Flag
		const int KEYEVENTF_KEYUP = 2; // Key Up Flag

		public static void SendKey(byte key)
		{
			keybd_event(key, 0, KEYEVENTF_EXTENDEDKEY, 0);
			keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
		}

		public static bool SendingKey;

		/// <summary>SHIFT: +(key), CTRL: ^(key), ALT %(key)</summary>
		public static void SendKey(string sKeys, string processName = null)
		{
			SendingKey = true;
			byte VK_NUMPAD0 = 0x60;
			byte VK_NUMPAD1 = 0x61;
			byte VK_NUMPAD2 = 0x62;
			byte VK_NUMPAD3 = 0x63;
			byte VK_NUMPAD4 = 0x64;
			byte VK_NUMPAD5 = 0x65;
			byte VK_NUMPAD6 = 0x66;
			byte VK_NUMPAD7 = 0x67;
			byte VK_NUMPAD8 = 0x68;
			byte VK_NUMPAD9 = 0x69;
			if (sKeys == "{NUM0}")
				SendKey(VK_NUMPAD0);
			else if (sKeys == "{NUM1}")
				SendKey(VK_NUMPAD1);
			else if (sKeys == "{NUM2}")
				SendKey(VK_NUMPAD2);
			else if (sKeys == "{NUM3}")
				SendKey(VK_NUMPAD3);
			else if (sKeys == "{NUM4}")
				SendKey(VK_NUMPAD4);
			else if (sKeys == "{NUM5}")
				SendKey(VK_NUMPAD5);
			else if (sKeys == "{NUM6}")
				SendKey(VK_NUMPAD6);
			else if (sKeys == "{NUM7}")
				SendKey(VK_NUMPAD7);
			else if (sKeys == "{NUM8}")
				SendKey(VK_NUMPAD8);
			else if (sKeys == "{NUM9}")
				SendKey(VK_NUMPAD9);
			//else if (sKeys == "{RM}" && !string.IsNullOrEmpty(processName))
			//	MouseHelper.SendRMouseClick(processName);
			//else if (sKeys == "{LM}" && !string.IsNullOrEmpty(processName))
			//	MouseHelper.SendLMouseClick(processName);
			else
				System.Windows.Forms.SendKeys.Send(sKeys);
			SendingKey = false;
		}

		public static void SendDown(params Key[] keys)
			=> Send(true, false, keys);

		public static void SendUp(params Key[] keys)
			=> Send(false, true, keys);

		public static void Send(params Key[] keys)
			=> Send(true, true, keys);

		/// <summary>
		/// Press down and up all specified keys.
		/// </summary>
		public static void Send(bool down, bool up, params Key[] keys)
		{
			if (down)
				foreach (var key in keys)
					keybd_event((byte)GetVKey(key), 0, KEYEVENTF_EXTENDEDKEY, 0);
			if (up)
				foreach (var key in keys)
					keybd_event((byte)GetVKey(key), 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
		}

		#endregion

		#region SendMessage

		#region Win32 - user32

		/// <summary>Maps a virtual key to a key code with specified keyboard.</summary>
		private const uint MAPVK_VK_TO_VSC_EX = 0x04;
		private const ushort KEY_PRESSED = 0xF000;

		// (Winuser.h) - Win32 apps - Microsoft Docs
		private const int KEY_DOWN = 0x0100;
		private const int KEY_UP = 0x0101;
		private const int VM_CHAR = 0x0102;

		/// <summary>Gets the key state of the specified key.</summary>
		/// <param name="nVirtKey">The key to check.</param>
		[DllImport("user32.dll")]
		private static extern ushort GetKeyState(int nVirtKey);

		/// <summary>
		/// Returns a handle to the foreground window.
		/// </summary>
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool SendMessage(IntPtr hWnd, int wMsg, uint wParam, uint lParam);

		[DllImport("user32.dll")]
		private static extern uint MapVirtualKey(uint uCode, uint uMapType);

		#endregion

		private static uint GetVKey(Key key)
			=> (uint)KeyInterop.VirtualKeyFromKey(key);

		public static void SendText(IntPtr hWnd, string msg, bool pressEnter = false)
		{
			foreach (char c in msg)
				SendChar(hWnd, c, true);
			if (pressEnter)
				SendMessage(hWnd, GetVKey(Key.Return), true);
		}

		public static bool SendChar(IntPtr hWnd, char c, bool checkKeyboardState)
		{
			if (checkKeyboardState)
				WaitWhileKeyModifierIsPressed();
			if (SendMessage(hWnd, VM_CHAR, c, 0))
				return false;
			return true;
		}

		public static bool SendMessage(IntPtr hWnd, uint key, bool checkKeyboardState, int delay = 100)
		{
			if (checkKeyboardState)
				WaitWhileKeyModifierIsPressed();
			if (SendMessage(hWnd, KEY_DOWN, key, GetLParam(1, key, 0, 0, 0, 0)))
				return false;
			Task.Delay(delay).Wait();
			if (SendMessage(hWnd, VM_CHAR, key, GetLParam(1, key, 0, 0, 0, 0)))
				return false;
			Task.Delay(delay).Wait();
			if (SendMessage(hWnd, KEY_UP, key, GetLParam(1, key, 0, 0, 1, 1)))
				return false;
			Task.Delay(delay).Wait();
			return true;
		}

		/// <summary>
		/// Send message and optionaly focus message and return window.
		/// </summary>
		public static bool SendTextMessage(
			string message, bool pressEnter = false,
			IntPtr messageWindowHandle = default,
			IntPtr returnWindowHandle = default
		)
		{
			// Try to set target message window as foreground if not set yet.
			if (messageWindowHandle != default || GetForegroundWindow() != returnWindowHandle)
			{
				if (!SetForegroundWindow(returnWindowHandle))
					return false;
			}
			SendText(returnWindowHandle, message, pressEnter);
			if (returnWindowHandle != default)
				SetForegroundWindow(messageWindowHandle);
			return true;
		}

		/// <summary>
		/// Wait untill all modifier keys are released.
		/// </summary>
		private static void WaitWhileKeyModifierIsPressed(CancellationToken cancellationToken = default)
		{
			while (IsPressed(
				Key.LeftAlt, Key.RightAlt,
				Key.LeftCtrl, Key.RightCtrl,
				Key.LeftShift, Key.RightShift,
				Key.LWin, Key.RWin)
			)
				Task.Delay(10, cancellationToken).Wait();
		}

		/// <summary>
		/// Return true if key is pressed.
		/// </summary>
		public static bool IsPressed(params Key[] keys)
		{
			foreach (var key in keys)
				if ((GetKeyState((int)GetVKey(key)) & KEY_PRESSED) == KEY_PRESSED)
					return true;
			return false;
		}

		/// <summary>
		/// Get lParam for SendMessage funcion.
		/// </summary>
		private static uint GetLParam(short repeatCount, uint vKey, byte extended, byte contextCode, byte previousState, byte transitionState)
		{
			var lParam = (uint)repeatCount;
			uint scanCode = MapVirtualKey(vKey, MAPVK_VK_TO_VSC_EX);
			lParam += scanCode * 0x10000;
			lParam += (uint)(extended * 0x1000000);
			lParam += (uint)(contextCode * 2 * 0x10000000);
			lParam += (uint)(previousState * 4 * 0x10000000);
			lParam += (uint)(transitionState * 8 * 0x10000000);
			return lParam;
		}

		#endregion

	}
}
