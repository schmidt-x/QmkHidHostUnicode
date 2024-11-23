using QmkHidHostUnicode.UnicodeSenders;
using System.Collections.Generic;
using QmkHidHostUnicode.Options;
using System.Diagnostics;
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
	
	private readonly Stopwatch _sw = new();
	private readonly LinkedList<uint> _codePoints = [];
	
	private const int Infinite = -1;
	private int _timeout = Infinite;

	private DeviceInfo _deviceInfo = null!;
	private Device _device = null!;
	
	public UnicodeListener(DeviceOptions deviceOpts, IUnicodeSender sender, ILogger logger)
	{
		_deviceOpts = deviceOpts;
		_sender = sender;
		_logger = logger;
	}
	
	public void Start()
	{
		var alias = _deviceOpts.Alias;
		
		_logger.Information("[{alias}] Connecting to device...", alias);
		
		_deviceInfo = FindDevice();
		_device = new Device(_deviceInfo.Path);
		
		_logger.Information(
			"[{alias}] Device connected ({deviceName})",
			alias, $"{_deviceInfo.ManufacturerString}: {_deviceInfo.ProductString}");
		
		byte reportId = _deviceOpts.ReportId;
		int keyRepeatFreq = _deviceOpts.RepeatFrequency;
		
		try
		{
			while (true)
			{
				// TODO: if the timeout is not too long, reduce time resolution for the 'device reading' period
				var input = ReadDevice(_timeout);
				
				if (input.IsEmpty) // timed out
				{
					Debug.Assert(_codePoints.Count != 0);
					
					SendCodepoint(_codePoints.Last!.Value);
					_timeout = keyRepeatFreq;
					continue;
				}
				
				if (input[0] != reportId) // wrong report id
				{
					if (_codePoints.Count == 0) continue;
					UpdateTimeoutAndSendIfExpiring(_codePoints.Last!.Value);
					continue;
				}
				
				uint codePoint = (uint)((input[1] << 16) | (input[2] << 8) | input[3]);
				if (codePoint > 0x10FFFF)
				{
					_logger.Warning("[{alias}] Invalid CodePoint: 0x{codePoint:X06}", alias, codePoint);
					continue;
				}
				
				bool isPressed = input[4] > 0;
				
				_logger.Information("[{alias}] CodePoint: 0x{codePoint:X06} " + (char)codePoint + " {state}",
					alias, codePoint, isPressed ? "Down" : "Up");
				
				if (isPressed)
					HandleDownEvent(codePoint);
				else
					HandleUpEvent(codePoint);
			}
		}
		finally { _device.Dispose(); }
	}
	
	
	private void HandleDownEvent(uint codePoint)
	{
		if (_codePoints.Count >= _deviceOpts.MaxPressedKeys)
		{
			_codePoints.RemoveFirst();
		}
		
		_codePoints.AddLast(codePoint);
		
		SendCodepoint(codePoint);
		_timeout = _deviceOpts.RepeatDelay;
	}
	
	private void HandleUpEvent(uint codePoint)
	{
		if (_codePoints.Count == 0) // no key was pressed down
		{
			Debug.Assert(_timeout == Infinite);
			return;
		}
		
		var last = _codePoints.Last!.Value;
		
		if (!_codePoints.Remove(codePoint)) // the key was never pressed or has been removed due to 'MaxPressedKeys'
		{
			UpdateTimeoutAndSendIfExpiring(last);
			return;
		}
		
		if (_codePoints.Count == 0)
		{
			_timeout = Infinite;
		}
		else if (last == codePoint) 
		{
			// Released key is the latest one.
			// Set timeout to 'RepeatDelay' before repeating the previous key. 
			_timeout = _deviceOpts.RepeatDelay;
		}
		else
		{
			// Released key is one of the previously held ones.
			// Keep repeating the last key.
			UpdateTimeoutAndSendIfExpiring(last);
		}
	}
	
	private void UpdateTimeoutAndSendIfExpiring(uint codePoint)
	{
		_timeout -= (int)_sw.ElapsedMilliseconds;
		
		// TODO: doesn't really make sense to set it to 1, due to default time resolution (10 to 15.6 ms)
		if (_timeout > 1) return;
		
		SendCodepoint(codePoint);
		_timeout = _deviceOpts.RepeatFrequency;
	}
	
	private void SendCodepoint(uint codePoint)
	{
		var exception = _sender.Send(codePoint);
		if (exception != null)
		{
			_logger.Warning("[{alias}] Error at sending: {errorMessage}", _deviceOpts.Alias, exception.Message);
		}
		_sw.Restart();
	}
	
	private DeviceInfo FindDevice()
	{
		while (true)
		{
			var deviceInfo = Hid.Enumerate(_deviceOpts.VendorId, _deviceOpts.ProductId)
				.FirstOrDefault(devInfo => devInfo.Usage == _deviceOpts.UsageId && devInfo.UsagePage == _deviceOpts.UsagePage);
			
			if (deviceInfo is not null) return deviceInfo;
			
			Thread.Sleep(_deviceOpts.ReconnectInterval);
		}
	}
	
	private ReadOnlySpan<byte> ReadDevice(int timeout) 
	{
		while (true)
		{
			try { return _device.ReadTimeout(5, timeout); }
			catch
			{
				_device.Dispose();
				_device = GetNewDevice();
			}
		}
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
				Thread.Sleep(_deviceOpts.ReconnectInterval);
				continue;
			}
			
			_logger.Information("[{alias}] Reconnected successfully", alias);
			return device;
		}
	}
}