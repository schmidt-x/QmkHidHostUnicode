using System.Runtime.InteropServices;
using System;

namespace QmkHidHostUnicode.UnicodeSenders.Windows;

public class SendInput : IUnicodeSender
{
	public Exception? Send(uint codePoint)
	{
		uint cInputs;
		Native.INPUT[] pInputs;
		
		if (codePoint > 0xFFFF)
		{
			cInputs = 4;
			pInputs = GetSurrogatePairInputs(codePoint);
		}
		else
		{
			cInputs = 2;
			pInputs = GetInputs((ushort)codePoint);
		}
		
		_ = Native.SendInput(cInputs, pInputs, Marshal.SizeOf<Native.INPUT>());
		
		return Marshal.GetLastPInvokeError() != 0
			? new Exception(Marshal.GetLastPInvokeErrorMessage())
			: null;
	}
	
	private static Native.INPUT[] GetSurrogatePairInputs(uint codePoint)
	{
		GetSurrogatePair(codePoint, out var highSurrogate, out var lowSurrogate);
		
		return 
		[
			new Native.INPUT
			{
				type = Native.INPUT_KEYBOARD,
				u = new Native.InputUnion
				{
					ki = new Native.KEYBDINPUT { wScan = highSurrogate, dwFlags = Native.KEYEVENTF_UNICODE }
				}
			},
			
			new Native.INPUT
			{
				type = Native.INPUT_KEYBOARD,
				u = new Native.InputUnion
				{
					ki = new Native.KEYBDINPUT { wScan = lowSurrogate, dwFlags = Native.KEYEVENTF_UNICODE }
				}
			},
			
			new Native.INPUT
			{
				type = Native.INPUT_KEYBOARD,
				u = new Native.InputUnion
				{
					ki = new Native.KEYBDINPUT
						{ wScan = highSurrogate, dwFlags = Native.KEYEVENTF_KEYUP | Native.KEYEVENTF_UNICODE }
				}
			},
			
			new Native.INPUT
			{
				type = Native.INPUT_KEYBOARD,
				u = new Native.InputUnion
				{
					ki = new Native.KEYBDINPUT 
						{ wScan = lowSurrogate, dwFlags = Native.KEYEVENTF_KEYUP | Native.KEYEVENTF_UNICODE }
				}
			}
		];
	}
	
	private static Native.INPUT[] GetInputs(ushort codePoint)
	{
		return
		[
			new Native.INPUT
			{
				type = Native.INPUT_KEYBOARD,
				u = new Native.InputUnion
				{
					ki = new Native.KEYBDINPUT { wScan = codePoint, dwFlags = Native.KEYEVENTF_UNICODE }
				}
			},
			
			new Native.INPUT
			{
				type = Native.INPUT_KEYBOARD,
				u = new Native.InputUnion
				{
					ki = new Native.KEYBDINPUT { wScan = codePoint, dwFlags = Native.KEYEVENTF_KEYUP | Native.KEYEVENTF_UNICODE }
				}
			}
		];
	}
	
	private static void GetSurrogatePair(uint codePoint, out ushort highSurrogate, out ushort lowSurrogate)
	{
		if (codePoint < 0x10000)
			throw new InvalidOperationException("CodePoint is less than 0x10000");
		
		uint sub = codePoint - 0x10000;
		
		highSurrogate = (ushort)((sub >> 10) + 0xD800);
		lowSurrogate = (ushort)((sub & 0x3FF) + 0xDC00);
	}
}