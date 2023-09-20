using UnityEngine;
using UnityEngine.Events;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  This behaviour fires UnityEvents for static TrackingManager events.
    /// </summary>
    /// <remarks>
    ///  This could for example be used to activate / disable certain GameObjects
    ///  depending on if the tracker has been initialized or stopped.
    /// </remarks>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Tracker Events Provider")]
    public class TrackerEventsProvider : MonoBehaviour
    {
        /// <summary>
        ///  Event fired when the tracker has been initialized.
        /// </summary>
        public UnityEvent trackerInitialized;

        /// <summary>
        ///  Event fired when the tracker has been stopped".
        /// </summary>
        public UnityEvent trackerStopped;

        void HandleTrackerInitialized()
        {
            this.trackerInitialized.Invoke();
        }

        void HandleTrackerStopped()
        {
            this.trackerStopped.Invoke();
        }

        void OnEnable()
        {
            TrackingManager.OnTrackerInitialized += HandleTrackerInitialized;
            TrackingManager.OnTrackerStopped += HandleTrackerStopped;
        }

        void OnDisable()
        {
            TrackingManager.OnTrackerInitialized -= HandleTrackerInitialized;
            TrackingManager.OnTrackerStopped -= HandleTrackerStopped;
        }
    }
}
