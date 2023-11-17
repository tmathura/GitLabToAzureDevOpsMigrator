using GitLabToAzureDevOpsMigrator.Core.Interfaces;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class ConsoleHelper : IConsoleHelper
    {
        public void DrawConsoleProgressBar(int progress, int total)
        {
            Console.CursorLeft = 0;

            const int progressBarLength = 100;
            var filledLength = (int)Math.Floor((double)progress / total * progressBarLength);

            Console.Write("[");
            Console.BackgroundColor = ConsoleColor.Green;
            Console.Write(new string(' ', filledLength));
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', progressBarLength - filledLength));
            Console.Write($"] {progress * 100 / total}%");
        }
    }
}
