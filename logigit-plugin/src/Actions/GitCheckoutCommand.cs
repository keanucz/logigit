namespace Loupedeck.LogiGitPlugin
{
    internal sealed class GitCheckoutCommand : GitCommandBase
    {
        public GitCheckoutCommand() : base(
            commandId: "git.checkout",
            displayName: "Checkout",
            description: "Checkout branch",
            groupName: "Git",
            iconResource: "tintOn.png",
            requiresCleanRepo: true)
        {
        }
    }
}

