namespace Loupedeck.LogiGitPlugin
{
    using System;

    public class SaturationToggle : PluginDynamicCommand
    {
        private const String ToggleId = "saturation";
        private Boolean _saturationToggled = false;
        private readonly String _imageResourcePathOn;
        private readonly String _imageResourcePathOff;
        private readonly AdjustmentSessionService _sessionService;

        public SaturationToggle()
            : base(displayName: "Saturation Switch", description: null, groupName: "Switches")
        {
            this._imageResourcePathOn = PluginResources.FindFile("saturationOn.png");
            this._imageResourcePathOff = PluginResources.FindFile("saturationOff.png");
            this._sessionService = PluginServiceRegistry.SessionService;
        }

        protected override void RunCommand(String actionParameter)
        {
            this._saturationToggled = !this._saturationToggled;
            this._sessionService.SelectToggle(ToggleId, this._saturationToggled);
            PluginLog.Info($"Saturation toggle {(this._saturationToggled ? "enabled" : "disabled")}");
            this.ActionImageChanged();
        }
        
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var resourcePath = this._saturationToggled ? this._imageResourcePathOn : this._imageResourcePathOff;
            return PluginResources.ReadImage(resourcePath);
        }
    }
}