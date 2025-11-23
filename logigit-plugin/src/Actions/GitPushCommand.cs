namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitPushCommand : GitCommandBase
    {
        public GitPushCommand() : base(
            commandId: "git.push",
            displayName: "Push",
            description: "Push current branch",
            groupName: "Git",
            iconResource: "redo.png",
            requiresCleanRepo: true)
        {
        }
    }
}

