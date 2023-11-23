using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Interfaces;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations;

public class ConsoleHelper : IConsoleHelper
{
    private IStopwatchWrapper Stopwatch { get; }
    private int Progress { get; set; }
    private readonly object _lock = new();

    public ConsoleHelper(IStopwatchWrapper stopwatch)
    {
        Stopwatch = stopwatch;
    }

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
                Console.Write($" Progress: {count}  ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write($"]  --> {GetElapsedTime()}");
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
                const int progressBarLength = 70;
                var filledLength = (int)Math.Floor((double)Progress / total * progressBarLength);
                Console.Write("[");
                Console.BackgroundColor = ConsoleColor.Green;
                Console.Write(new string(' ', filledLength));
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(new string(' ', progressBarLength - filledLength));
                Console.Write($"] {Progress * 100 / total}%  --> {GetElapsedTime()}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }
    }

    private string GetElapsedTime()
    {
        return $"Elapsed Time: {Stopwatch.Elapsed.Hours}{Stopwatch.Elapsed.Minutes}:{Stopwatch.Elapsed.Seconds}:{Stopwatch.Elapsed.Milliseconds}";
    }
}