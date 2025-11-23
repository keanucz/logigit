namespace Loupedeck.LogiGitPlugin
{
    using System;

    public class ContrastToggle : PluginDynamicCommand
    {
        private const String ToggleId = "contrast";
        private Boolean _contrastToggled = false;
        private readonly String _imageResourcePathOn;
        private readonly String _imageResourcePathOff;
        private readonly AdjustmentSessionService _sessionService;

        public ContrastToggle()
            : base(displayName: "Contrast Switch", description: null, groupName: "Switches")
        {
            this._imageResourcePathOn = PluginResources.FindFile("contrastOn.png");
            this._imageResourcePathOff = PluginResources.FindFile("contrastOff.png");
            this._sessionService = PluginServiceRegistry.SessionService;
        }

        protected override void RunCommand(String actionParameter)
        {
            this._contrastToggled = !this._contrastToggled;
            this._sessionService.SelectToggle(ToggleId, this._contrastToggled);
            PluginLog.Info($"Contrast toggle {(this._contrastToggled ? "enabled" : "disabled")}");
            this.ActionImageChanged();
        }
        
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var resourcePath = this._contrastToggled ? this._imageResourcePathOn : this._imageResourcePathOff;
            return PluginResources.ReadImage(resourcePath);
        }
    }
}