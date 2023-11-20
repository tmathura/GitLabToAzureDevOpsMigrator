using GitLabToAzureDevOpsMigrator.Core.Interfaces;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class ConsoleHelper : IConsoleHelper
    {
        private int Progress { get; set; }
        private readonly object _lock = new();

        public void DrawConsoleProgressCount(int count)
        {
            if (Monitor.TryEnter(_lock))
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.CursorLeft = 0;
                    Console.Write("[");
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.Write($" Progress: {count}");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write("]");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
        }

        public void DrawConsoleProgressBar(int total)
        {
            if (Monitor.TryEnter(_lock))
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Progress++;
                    Console.CursorLeft = 0;
                    const int progressBarLength = 100;
                    var filledLength = (int)Math.Floor((double)Progress / total * progressBarLength);
                    Console.Write("[");
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.Write(new string(' ', filledLength));
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(new string(' ', progressBarLength - filledLength));
                    Console.Write($"] {Progress * 100 / total}%");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
        }
    }
}
