using QmkHidHostUnicode.UnicodeSenders;
using QmkHidHostUnicode.Options;
using System.Threading;
using System.Linq;
using Serilog;
using System;
using HidApi;

namespace QmkHidHostUnicode;

public class UnicodeListener
{
	private readonly DeviceOptions _deviceOpts;
	private readonly IUnicodeSender _sender;
	private readonly ILogger _logger;

	private DeviceInfo _deviceInfo = null!;
	private Device _device = null!;
	
	public UnicodeListener(DeviceOptions deviceOpts, IUnicodeSender sender, ILogger logger)
	{
		_deviceOpts = deviceOpts;
		_sender = sender;
		_logger = logger;
	}
	
	public void StartListening()
	{
		var alias = _deviceOpts.Alias;
		
		_logger.Information("[{alias}] Connecting to device...", alias);
		
		_deviceInfo = FindDevice();
		_device = new Device(_deviceInfo.Path);
		
		_logger.Information(
			"[{alias}] Device connected ({deviceName})",
			alias, $"{_deviceInfo.ManufacturerString}: {_deviceInfo.ProductString}");
		
		var hidUnicodeReportId = _deviceOpts.HidUnicodeReportId;
		
		try
		{
			while (true)
			{
				var input = ReadDevice();
				
				var id = input[0];
				if (id != hidUnicodeReportId) continue;
				
				var codePoint = (uint)((input[1] << 16) | (input[2] << 8) | input[3]);
				if (codePoint > 0x10FFFF)
				{
					_logger.Warning("[{alias}] Invalid CodePoint: 0x{codePoint:X06}", alias, codePoint);
					continue;
				}
				
				_logger.Information("[{alias}] CodePoint: 0x{codePoint:X06}", alias, codePoint);
				
				var exception = _sender.Send(codePoint);
				if (exception != null)
				{
					_logger.Warning("[{alias}] Error at sending: {errorMessage}", alias, exception.Message);
				}
			}
		}
		finally { _device.Dispose(); }
	}
	
	
	private DeviceInfo FindDevice()
	{
		while (true)
		{
			var deviceInfo = Hid.Enumerate(_deviceOpts.VendorId, _deviceOpts.ProductId)
				.FirstOrDefault(devInfo => devInfo.Usage == _deviceOpts.UsageId && devInfo.UsagePage == _deviceOpts.UsagePage);
			
			if (deviceInfo is not null) return deviceInfo;
			
			Thread.Sleep(_deviceOpts.ReconnectDelay);
		}
	}
	
	private ReadOnlySpan<byte> ReadDevice()
	{
		ReadOnlySpan<byte> input;
		
		while (true)
		{
			// TODO: do I have to read all the buffer?
			try { input = _device.Read(32); }
			catch
			{
				_device.Dispose();
				_device = GetNewDevice();
				continue;
			}
			
			break;
		}
		
		return input;
	}
		
	private Device GetNewDevice()
	{
		var alias = _deviceOpts.Alias;
			
		while (true)
		{
			_logger.Warning("[{alias}] Attempting to reconnect...", alias);
			
			Device device;
			
			try { device = new Device(_deviceInfo.Path); }
			catch
			{
				Thread.Sleep(_deviceOpts.ReconnectDelay);
				continue;
			}
			
			_logger.Information("[{alias}] Reconnected successfully", alias);
			return device;
		}
	}
}