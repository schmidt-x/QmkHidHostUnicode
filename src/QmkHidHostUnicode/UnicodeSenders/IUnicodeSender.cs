using System;

namespace QmkHidHostUnicode.UnicodeSenders;

public interface IUnicodeSender
{
	public Exception? Send(uint codePoint);
}