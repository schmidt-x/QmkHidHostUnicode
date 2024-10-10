using System.Runtime.InteropServices;
using System;

namespace QmkHidHostUnicode.UnicodeSenders.Windows;

public class SendInput : IUnicodeSender
{
	public Exception? Send(uint codePoint)
	{
		uint cInputs;
		Native.INPUT[] pInputs;
		
		switch (codePoint)
		{
			case <= 0xFFFF:
				cInputs = 2;
				pInputs = GetInputs((ushort)codePoint);
				break;
			
			case <= 0x10FFFF:
				cInputs = 4;
				pInputs = GetSurrogatePairInputs(codePoint);
				break;
			
			default:
				return new Exception($"Invalid CodePoint: 0x{codePoint:X06}");
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
			Native.INPUT.NewKeyboardInput(wScan: highSurrogate, dwFlags: Native.KEYEVENTF_UNICODE),
			Native.INPUT.NewKeyboardInput(wScan: lowSurrogate,  dwFlags: Native.KEYEVENTF_UNICODE),
			Native.INPUT.NewKeyboardInput(wScan: highSurrogate, dwFlags: Native.KEYEVENTF_UNICODE | Native.KEYEVENTF_KEYUP),
			Native.INPUT.NewKeyboardInput(wScan: lowSurrogate,  dwFlags: Native.KEYEVENTF_UNICODE | Native.KEYEVENTF_KEYUP)
		];
	}
	
	private static Native.INPUT[] GetInputs(ushort codePoint) =>
		[
			Native.INPUT.NewKeyboardInput(wScan: codePoint, dwFlags: Native.KEYEVENTF_UNICODE),
			Native.INPUT.NewKeyboardInput(wScan: codePoint, dwFlags: Native.KEYEVENTF_UNICODE | Native.KEYEVENTF_KEYUP)
		];
	
	private static void GetSurrogatePair(uint codePoint, out ushort highSurrogate, out ushort lowSurrogate)
	{
		if (codePoint < 0x10000)
			throw new InvalidOperationException("CodePoint is less than 0x10000");
		
		uint sub = codePoint - 0x10000;
		
		highSurrogate = (ushort)((sub >> 10) + 0xD800);
		lowSurrogate = (ushort)((sub & 0x3FF) + 0xDC00);
	}
}