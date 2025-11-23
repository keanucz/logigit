namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitStashCommand : GitCommandBase
    {
        public GitStashCommand() : base(
            commandId: "git.stash",
            displayName: "Stash",
            description: "Create git stash",
            groupName: "Git",
            iconResource: "save.png",
            requiresCleanRepo: false)
        {
        }
    }
}

