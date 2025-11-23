namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitPullCommand : GitCommandBase
    {
        public GitPullCommand() : base(
            commandId: "git.pull",
            displayName: "Pull",
            description: "Pull latest changes",
            groupName: "Git",
            iconResource: "contrastOn.png",
            requiresCleanRepo: false)
        {
        }
    }
}

