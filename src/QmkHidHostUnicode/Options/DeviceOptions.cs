namespace QmkHidHostUnicode.Options;

public class DeviceOptions
{
	public const string Section = "Devices";
	
	public string Alias { get; init; } = "Undefined";
	public ushort VendorId { get; init; }
	public ushort ProductId { get; init; }
	public ushort UsageId { get; init; }
	public ushort UsagePage { get; init; }
	public byte HidUnicodeReportId { get; init; }
	public int ReconnectDelay { get; init; }
}