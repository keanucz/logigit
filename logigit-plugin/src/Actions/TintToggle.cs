namespace Loupedeck.LogiGitPlugin
{
    using System;

    public class TintToggle : PluginDynamicCommand
    {
        private const String ToggleId = "tint";
        private Boolean _tintToggled = false;
        private readonly String _imageResourcePathOn;
        private readonly String _imageResourcePathOff;
        private readonly AdjustmentSessionService _sessionService;

        public TintToggle()
            : base(displayName: "Tint Switch", description: null, groupName: "Switches")
        {
            this._imageResourcePathOn = PluginResources.FindFile("tintOn.png");
            this._imageResourcePathOff = PluginResources.FindFile("tintOff.png");
            this._sessionService = PluginServiceRegistry.SessionService;
        }

        protected override void RunCommand(String actionParameter)
        {
            this._tintToggled = !this._tintToggled;
            this._sessionService.SelectToggle(ToggleId, this._tintToggled);
            PluginLog.Info($"Tint toggle {(this._tintToggled ? "enabled" : "disabled")}");
            this.ActionImageChanged();
        }
        
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var resourcePath = this._tintToggled ? this._imageResourcePathOn : this._imageResourcePathOff;
            return PluginResources.ReadImage(resourcePath);
        }
    }
}