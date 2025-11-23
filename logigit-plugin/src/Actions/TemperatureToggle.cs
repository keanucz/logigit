namespace Loupedeck.LogiGitPlugin
{
    using System;

    public class TemperatureToggle : PluginDynamicCommand
    {
        private const String ToggleId = "temperature";
        private Boolean _tempToggled = false;
        private readonly String _imageResourcePathOn;
        private readonly String _imageResourcePathOff;
        private readonly AdjustmentSessionService _sessionService;

        public TemperatureToggle()
            : base(displayName: "Temperature Switch", description: null, groupName: "Switches")
        {
            this._imageResourcePathOn = PluginResources.FindFile("temperatureOn.png");
            this._imageResourcePathOff = PluginResources.FindFile("temperatureOff.png");
            this._sessionService = PluginServiceRegistry.SessionService;
        }

        protected override void RunCommand(String actionParameter)
        {
            this._tempToggled = !this._tempToggled;
            this._sessionService.SelectToggle(ToggleId, this._tempToggled);
            PluginLog.Info($"Temperature toggle {(this._tempToggled ? "enabled" : "disabled")}");
            this.ActionImageChanged();
        }
        
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var resourcePath = this._tempToggled ? this._imageResourcePathOn : this._imageResourcePathOff;
            return PluginResources.ReadImage(resourcePath);
        }
    }
}