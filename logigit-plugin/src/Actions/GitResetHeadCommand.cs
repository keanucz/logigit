namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitResetHeadCommand : GitCommandBase
    {
        public GitResetHeadCommand() : base(
            commandId: "git.reset.head",
            displayName: "Reset HEAD~1",
            description: "Reset to previous commit",
            groupName: "Git",
            iconResource: "highlightOff.png",
            requiresCleanRepo: true)
        {
        }
    }
}

