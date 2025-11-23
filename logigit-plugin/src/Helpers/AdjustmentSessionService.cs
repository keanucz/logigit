namespace Loupedeck.LogiGitPlugin
{
    using System;

    /// <summary>
    /// Tracks which toggle is currently active and publishes dial activity events.
    /// </summary>
    internal sealed class AdjustmentSessionService
    {
        private readonly Object _syncRoot = new();
        private String _activeToggleId;

        /// <summary>
        /// Raised whenever the active toggle changes (or is cleared).
        /// </summary>
        public event EventHandler<SessionChangedEventArgs> SessionChanged;

        /// <summary>
        /// Raised whenever the dial value shifts. Includes the current toggle snapshot.
        /// </summary>
        public event EventHandler<DialChangedEventArgs> DialChanged;

        public String ActiveToggleId
        {
            get
            {
                lock (this._syncRoot)
                {
                    return this._activeToggleId;
                }
            }
        }

        public void SelectToggle(String toggleId, Boolean isEnabled)
        {
            toggleId = toggleId ?? throw new ArgumentNullException(nameof(toggleId));

            String previous;
            String next = isEnabled ? toggleId : null;

            lock (this._syncRoot)
            {
                previous = this._activeToggleId;
                this._activeToggleId = next;
            }

            if (!String.Equals(previous, next, StringComparison.Ordinal))
            {
                this.SessionChanged?.Invoke(this, new SessionChangedEventArgs(next));
            }
        }

        public void UpdateDial(Int32 value, Int32 diff)
        {
            var toggleSnapshot = this.ActiveToggleId;
            var args = new DialChangedEventArgs(toggleSnapshot, value, diff, DateTimeOffset.UtcNow);
            this.DialChanged?.Invoke(this, args);
        }
    }

    internal sealed class SessionChangedEventArgs : EventArgs
    {
        public SessionChangedEventArgs(String toggleId)
        {
            this.ToggleId = toggleId;
        }

        public String ToggleId { get; }

        public Boolean HasActiveToggle => !String.IsNullOrEmpty(this.ToggleId);
    }

    internal sealed class DialChangedEventArgs : EventArgs
    {
        public DialChangedEventArgs(String toggleId, Int32 value, Int32 diff, DateTimeOffset timestamp)
        {
            this.ToggleId = toggleId;
            this.Value = value;
            this.Diff = diff;
            this.Timestamp = timestamp;
        }

        public String ToggleId { get; }

        public Int32 Value { get; }

        public Int32 Diff { get; }

        public DateTimeOffset Timestamp { get; }

        public Boolean HasActiveToggle => !String.IsNullOrEmpty(this.ToggleId);
    }
}
