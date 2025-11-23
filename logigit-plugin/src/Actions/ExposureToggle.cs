namespace Loupedeck.LogiGitPlugin
{
    using System;

    public class ExposureToggle : PluginDynamicCommand
    {
        private const String ToggleId = "exposure";
        private Boolean _exposureToggled = false;
        private readonly String _imageResourcePathOn;
        private readonly String _imageResourcePathOff;
        private readonly AdjustmentSessionService _sessionService;

        public ExposureToggle()
            : base(displayName: "Exposure Switch", description: null, groupName: "Switches")
        {
            this._imageResourcePathOn = PluginResources.FindFile("exposureOn.png");
            this._imageResourcePathOff = PluginResources.FindFile("exposureOff.png");
            this._sessionService = PluginServiceRegistry.SessionService;
        }

        protected override void RunCommand(String actionParameter)
        {
            this._exposureToggled = !this._exposureToggled;
            this._sessionService.SelectToggle(ToggleId, this._exposureToggled);
            PluginLog.Info($"Exposure toggle {(this._exposureToggled ? "enabled" : "disabled")}");
            this.ActionImageChanged();
        }
        
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var resourcePath = this._exposureToggled ? this._imageResourcePathOn : this._imageResourcePathOff;
            return PluginResources.ReadImage(resourcePath);
        }
    }
}