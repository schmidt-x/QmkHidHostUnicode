using System.Runtime.InteropServices;
using System;

namespace QmkHidHostUnicode.UnicodeSenders.Windows;

internal partial class Native
{
	[LibraryImport("user32.dll", SetLastError = true)]
	internal static partial uint SendInput(
		uint cInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
	
	internal const int INPUT_KEYBOARD = 1;
	
	internal const uint KEYEVENTF_KEYUP   = 0x02;
	internal const uint KEYEVENTF_UNICODE = 0x04;
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct INPUT
	{
		internal uint type;
		internal InputUnion u;
	}
	
	[StructLayout(LayoutKind.Explicit)]
	internal struct InputUnion
	{
		[FieldOffset(0)]
		internal MOUSEINPUT mi;
		
		[FieldOffset(0)]
		internal KEYBDINPUT ki;
		
		[FieldOffset(0)]
		internal HARDWAREINPUT hi;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct MOUSEINPUT
	{
		internal int dx;
		internal int dy;
		internal uint mouseData;
		internal uint dwFlags;
		internal uint time;
		internal IntPtr dwExtraInfo;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct KEYBDINPUT
	{
		internal ushort wVk;
		internal ushort wScan;
		internal uint dwFlags;
		internal uint time;
		internal IntPtr dwExtraInfo;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct HARDWAREINPUT
	{
		internal uint uMsg;
		internal ushort wParamL;
		internal ushort wParamH;
	}
}