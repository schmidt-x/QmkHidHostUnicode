using System;

namespace QmkHidHostUnicode.UnicodeSenders;

public class UnicodeSelector
{
	public static IUnicodeSender GetSender()
	{
		if (OperatingSystem.IsWindows())
		{
			return new Windows.SendInput();
		}
		
		if (OperatingSystem.IsLinux())
		{
			throw new NotImplementedException();
		}
		
		if (OperatingSystem.IsMacOS())
		{
			throw new NotImplementedException();
		}
		
		throw new PlatformNotSupportedException();
	}
}