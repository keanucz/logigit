namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitStatusCommand : GitCommandBase
    {
        public GitStatusCommand() : base(
            commandId: "git.status",
            displayName: "Status",
            description: "Show git status",
            groupName: "Git",
            iconResource: "temperatureOn.png",
            requiresCleanRepo: false)
        {
        }
    }
}

