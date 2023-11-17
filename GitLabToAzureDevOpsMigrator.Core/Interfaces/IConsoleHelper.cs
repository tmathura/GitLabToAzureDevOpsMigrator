namespace GitLabToAzureDevOpsMigrator.Core.Interfaces;

public interface IConsoleHelper
{
    void DrawConsoleProgressBar(int progress, int total);
}