using System;

namespace LuaCP.Reporting
{
	public enum ReportLevel : byte
	{
		Notice = 1,
		Warning = 2,
		Error = 3,
		FatalError = 4,
	}

	public interface IReporter
	{
		bool AtLeastLevel(ReportLevel level);

		void Report(ReportLevel level, string message);

		void Report(ReportLevel level, string message, Range range);
	}

	public class ConsoleReporter : IReporter
	{
		private byte maxLevel = 0;

		public bool AtLeastLevel(ReportLevel level)
		{
			return maxLevel >= (int)level;
		}

		public void Report(ReportLevel level, string message)
		{
			WithColor(GetColor(level), String.Format("[{0}]: {1}", level, message));
			maxLevel = Math.Max(maxLevel, (byte)level);
		}

		public void Report(ReportLevel level, string message, Range range)
		{
			WithColor(GetColor(level), String.Format("[{0}]: ({1}) {2}", level, range, message));
			maxLevel = Math.Max(maxLevel, (byte)level);
		}

		private void WithColor(ConsoleColor color, string message)
		{
			ConsoleColor oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(message);
			Console.ForegroundColor = oldColor;
		}

		private ConsoleColor GetColor(ReportLevel level)
		{
			switch (level)
			{
				case ReportLevel.Notice:
					return ConsoleColor.DarkGray;
				case ReportLevel.Warning:
					return ConsoleColor.Yellow;
				case ReportLevel.Error:
				case ReportLevel.FatalError:
					return ConsoleColor.Red;
				default:
					return ConsoleColor.White;
			}
		}
	}
}

