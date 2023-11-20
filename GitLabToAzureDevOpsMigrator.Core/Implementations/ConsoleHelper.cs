using GitLabToAzureDevOpsMigrator.Core.Interfaces;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class ConsoleHelper : IConsoleHelper
    {
        private int Progress { get; set; }
        
        public void DrawConsoleProgressCount(int count)
        {
            Console.CursorLeft = 0;
            
            Console.Write("[");
            Console.BackgroundColor = ConsoleColor.Green;
            Console.Write($" Progress: {count}");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("]");
        }

        public void DrawConsoleProgressBar(int total)
        {
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
        }
    }
}
