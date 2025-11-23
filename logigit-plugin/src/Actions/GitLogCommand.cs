namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitLogCommand : GitCommandBase
    {
        public GitLogCommand() : base(
            commandId: "git.log",
            displayName: "Git Log",
            description: "Show git history",
            groupName: "Git",
            iconResource: "saturationOn.png",
            requiresCleanRepo: false)
        {
        }
    }
}

