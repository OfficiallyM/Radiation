using System.IO;
using TLDLoader;

namespace Radiation.Modules
{
	internal static class Logger
	{
		private static string logFile = "";
		private static bool initialised = false;
		public enum LogLevel
		{
			Debug,
			Info,
			Warning,
			Error,
			Critical
		}

		public static void Init()
		{
			if (!initialised)
			{
				// Create logs directory.
				if (Directory.Exists(ModLoader.ModsFolder))
				{
					Directory.CreateDirectory(Path.Combine(ModLoader.ModsFolder, "Logs"));
					logFile = ModLoader.ModsFolder + $"\\Logs\\{Radiation.mod.ID}.log";
					File.WriteAllText(logFile, $"{Radiation.mod.Name} v{Radiation.mod.Version} initialised\r\n");
					initialised = true;
				}
			}
		}

		/// <summary>
		/// Log messages to a file.
		/// </summary>
		/// <param name="msg">The message to log</param>
		public static void Log(string msg, LogLevel logLevel = LogLevel.Info)
		{
			// Don't print debug messages outside of debug mode.
			if (!Radiation.debug && logLevel == LogLevel.Debug) return;

			if (logFile != string.Empty)
				File.AppendAllText(logFile, $"[{logLevel}] {msg}\r\n");
		}
	}
}
