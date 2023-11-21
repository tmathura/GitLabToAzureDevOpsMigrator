namespace GitLabToAzureDevOpsMigrator.Domain.Interfaces;

public interface IStopwatchWrapper
{
    TimeSpan Elapsed { get; }

    void Start();
    void Stop();
    void Reset();
}