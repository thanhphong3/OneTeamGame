using UnityEngine;
using UnityEngine.Events;
using System;
using Visometry.VisionLib.SDK.Core.API;
using UnityEngine.Serialization;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  This behaviour fires UnityEvents for TrackingManager.OnTrackingStates
    ///  events.
    /// </summary>
    /// <remarks>
    ///  This could for example be used to activate / disable certain GameObjects
    ///  depending on the current tracking state.
    /// </remarks>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Tracking State Provider")]
    public class TrackingStateProvider : MonoBehaviour
    {
        [Tooltip(
            "Name of the object which's tracking states should be observed." +
            "\nShould be 'TrackedObject' for Single Model Tracking, the anchor name for Multi Model Tracking.")]
        public string trackingAnchorName = "TrackedObject";

        /// <summary>
        ///  Event fired once after the tracking state changed to "tracked".
        /// </summary>
        [FormerlySerializedAs("justTrackedEvent")]
        public UnityEvent tracked;

        /// <summary>
        ///  Event fired once after the tracking state changed to "critical".
        /// </summary>
        [FormerlySerializedAs("justCriticalEvent")]
        public UnityEvent trackingCritical;

        /// <summary>
        ///  Event fired once after the tracking state changed to "lost".
        /// </summary>
        [FormerlySerializedAs("justLostEvent")]
        public UnityEvent trackingLost;

        private string previousState = "";

        void HandleTrackerInitializing()
        {
            this.previousState = "";
        }

        void HandleTrackingStates(TrackingState state)
        {
            for (int i = 0; i < state.objects.Length; ++i)
            {
                TrackingState.TrackingObject obj = state.objects[i];

                if (obj.name == this.trackingAnchorName)
                {
                    if (obj.state == "tracked")
                    {
                        if (this.previousState != obj.state)
                        {
                            this.tracked.Invoke();
                        }
                    }
                    else if (obj.state == "critical")
                    {
                        if (this.previousState != obj.state)
                        {
                            this.trackingCritical.Invoke();
                        }
                    }
                    else if (obj.state == "lost")
                    {
                        if (this.previousState != obj.state)
                        {
                            this.trackingLost.Invoke();
                        }
                    }

                    this.previousState = obj.state;

                    break;
                }
            }
        }

        void OnEnable()
        {
            TrackingManager.OnTrackerInitializing += HandleTrackerInitializing;
            TrackingManager.OnTrackingStates += HandleTrackingStates;
        }

        void OnDisable()
        {
            TrackingManager.OnTrackingStates -= HandleTrackingStates;
            TrackingManager.OnTrackerInitializing -= HandleTrackerInitializing;
        }
    }
}
