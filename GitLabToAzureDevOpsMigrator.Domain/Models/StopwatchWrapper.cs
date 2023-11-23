using GitLabToAzureDevOpsMigrator.Domain.Interfaces;
using System.Diagnostics;

namespace GitLabToAzureDevOpsMigrator.Domain.Models;

public class StopwatchWrapper : IStopwatchWrapper
{
    private Stopwatch Stopwatch { get; } = new();

    public TimeSpan Elapsed => Stopwatch.Elapsed;

    public void Start()
    {
        Stopwatch.Start();
    }

    public void Stop()
    {
        Stopwatch.Stop();
    }

    public void Reset()
    {
        Stopwatch.Reset();
    }
}