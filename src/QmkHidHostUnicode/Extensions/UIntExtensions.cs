namespace QmkHidHostUnicode.Extensions;

internal static class UIntExtensions
{
	internal static string ToUtf16String(this uint value)
	{
		if (value < 0x10000) return ((char)value).ToString();
		
		uint sub = value - 0x10000;
		var high = (ushort)((sub >> 10) + 0xD800);
		var low  = (ushort)((sub & 0x3FF) + 0xDC00);
		
		return $"{(char)high}{(char)low}";
	}
}