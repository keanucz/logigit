namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitStashPopCommand : GitCommandBase
    {
        public GitStashPopCommand() : base(
            commandId: "git.stash.pop",
            displayName: "Stash Pop",
            description: "Apply latest stash",
            groupName: "Git",
            iconResource: "undo.png",
            requiresCleanRepo: true)
        {
        }
    }
}

