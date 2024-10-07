using Microsoft.Extensions.Configuration;
using QmkHidHostUnicode.UnicodeSenders;
using System.Collections.Generic;
using QmkHidHostUnicode.Options;
using System.Threading;
using System.IO;
using Serilog;
using System;

namespace QmkHidHostUnicode;

public class Program
{
	private static void Main()
	{
		try { HelloWorld(); }
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message + "\r\nPress any key to close the console");
			Console.ReadKey();
		}
	}
	
	private static void HelloWorld()
	{
		var config = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json")
			.Build();
		
		var sender = UnicodeSelector.GetSender();
		var logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();
		
		var threads = new List<Thread>();
		
		foreach (var childSection in config.GetSection(DeviceOptions.Section).GetChildren())
		{
			var deviceOpts = new DeviceOptions();
			childSection.Bind(deviceOpts);
			
			var unicodeListener = new UnicodeListener(deviceOpts, sender, logger);
			
			var thread = new Thread(unicodeListener.StartListening);
			thread.Start();
			threads.Add(thread);
		}
		
		foreach (var thread in threads)
			thread.Join();
	}
}
