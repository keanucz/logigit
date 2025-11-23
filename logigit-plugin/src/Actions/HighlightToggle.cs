namespace Loupedeck.LogiGitPlugin
{
    using System;

    public class HighlightToggle : PluginDynamicCommand
    {
        private const String ToggleId = "highlight";
        private Boolean _highlightToggled = false;
        private readonly String _imageResourcePathOn;
        private readonly String _imageResourcePathOff;
        private readonly AdjustmentSessionService _sessionService;

        public HighlightToggle()
            : base(displayName: "Highlight Switch", description: null, groupName: "Switches")
        {
            this._imageResourcePathOn = PluginResources.FindFile("highlightOn.png");
            this._imageResourcePathOff = PluginResources.FindFile("highlightOff.png");
            this._sessionService = PluginServiceRegistry.SessionService;
        }

        protected override void RunCommand(String actionParameter)
        {
            this._highlightToggled = !this._highlightToggled;
            this._sessionService.SelectToggle(ToggleId, this._highlightToggled);
            PluginLog.Info($"Highlight toggle {(this._highlightToggled ? "enabled" : "disabled")}");
            this.ActionImageChanged();
        }
        
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var resourcePath = this._highlightToggled ? this._imageResourcePathOn : this._imageResourcePathOff;
            return PluginResources.ReadImage(resourcePath);
        }
    }
}