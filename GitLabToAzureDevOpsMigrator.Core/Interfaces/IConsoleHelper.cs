namespace GitLabToAzureDevOpsMigrator.Core.Interfaces;

public interface IConsoleHelper
{
    void DrawConsoleProgressCount(int count);
    void DrawConsoleProgressBar(int total);
    void ResetProgressBar();
}