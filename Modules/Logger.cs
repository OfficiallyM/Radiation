using System.IO;
using TLDLoader;

namespace Radiation.Modules
{
	internal static class Logger
	{
		private static string _logFile = "";
		private static bool _initialised = false;
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
			if (!_initialised)
			{
				// Create logs directory.
				if (Directory.Exists(ModLoader.ModsFolder))
				{
					Directory.CreateDirectory(Path.Combine(ModLoader.ModsFolder, "Logs"));
					_logFile = ModLoader.ModsFolder + $"\\Logs\\{Radiation.mod.ID}.log";
					File.WriteAllText(_logFile, $"{Radiation.mod.Name} v{Radiation.mod.Version} initialised\r\n");
					_initialised = true;
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

			if (_logFile != string.Empty)
				File.AppendAllText(_logFile, $"[{logLevel}] {msg}\r\n");
		}
	}
}
