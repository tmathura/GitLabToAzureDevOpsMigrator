using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using log4net;
using NGitLab;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.GitLab
{
    public class MilestoneBl : IMilestoneBl
    {
        private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        private IConsoleHelper ConsoleHelper { get; }
        private IMilestoneClient MilestoneClient { get; }

        public MilestoneBl(IConsoleHelper consoleHelper, IMilestoneClient milestoneClient)
        {
            ConsoleHelper = consoleHelper;
            MilestoneClient = milestoneClient;
        }
        
        public List<Cycle>? Get()
        {
            const string startingProcessMessage = "Started getting GitLab milestones.";

            Console.WriteLine(startingProcessMessage);
            Logger.Info(startingProcessMessage);

            try
            {
                var milestones = MilestoneClient.All;

                var count = 0;
                var errorCount = 0;
                var cycles = new List<Cycle>();

                foreach (var milestone in milestones)
                {
                    try
                    {
                        count++;
                        cycles.Add(new Cycle(milestone, null));

                        ConsoleHelper.DrawConsoleProgressCount(count);
                    }
                    catch (Exception exception)
                    {
                        errorCount++;

                        Logger.Error($"Error getting GitLab milestone #{milestone.Id} - '{milestone.Title}', was on milestone count: {count}.", exception);

                        count--;
                    }
                }

                var endingProcessMessage = $"{Environment.NewLine}Finished getting GitLab milestones, there were {count} milestones retrieved & there were errors getting {errorCount} milestones.";

                Console.WriteLine(endingProcessMessage);
                Logger.Info(endingProcessMessage);

                return cycles;
            }
            catch (Exception exception)
            {
                Logger.Error($"Getting GitLab milestones encountered a problem: {exception.Message}", exception);

                return null;
            }
        }
    }
}