using System;

namespace QmkHidHostUnicode.Options;

public class DeviceOptions
{
	public const string Section = "Devices";
	
	public string Alias { get; init; } = "Undefined";
	public ushort VendorId { get; init; }
	public ushort ProductId { get; init; }
	public ushort UsagePage { get; init; } = 0xFF60;
	public ushort UsageId { get; init; } = 0x61;
	public byte ReportId { get; init; }
	public int ReconnectInterval { get; init; }
	
	private readonly int _repeatDelay;
	public int RepeatDelay
	{
		get => _repeatDelay;
		init => _repeatDelay = Math.Max(value, 1);
	}

	private readonly int _repeatFrequency;
	public int RepeatFrequency
	{
		get => _repeatFrequency;
		init => _repeatFrequency = Math.Max(value, 1);
	}
	
	private readonly int _maxPressedKeys;
	public int MaxPressedKeys
	{
		get => _maxPressedKeys;
		init => _maxPressedKeys = Math.Max(value, 1);
	}
}