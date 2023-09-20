using System;
using System.Threading.Tasks;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    /// If your model is located on a flat surface and will never be titled
    /// relative to the worlds horizon, you can improve the tracking results
    /// in some cases by using the plane constrained mode.
    ///
    /// As long as this is enabled, the model tracker will only try to find
    /// poses that align the models up vector with the worlds up vector.
    ///
    /// Only works with ARCore, ARKit, ARFoundation, HoloLens or other
    /// external SLAM sources. Extendible tracking has to be turned on.
    ///  **THIS IS SUBJECT TO CHANGE** Please do not rely on this code in productive environments.
    /// </summary>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Plane Constrained Mode")]
    public class PlaneConstrainedMode : TrackingManagerReference
    {
        [Tooltip("The up vector of the model.")]
        public Vector3 modelUpVector = new Vector3(0, 1, 0);
        [Tooltip("Center of the model. This point is used to rotate the init pose around.")]
        public Vector3 modelCenter = new Vector3(0, 0, 0);
        [Tooltip("The up vector of the world.")]
        public Vector3 worldUpVector = new Vector3(0, 1, 0);

        private async Task SetConstraintInTrackerAsync()
        {
            await ModelTrackerCommands.Set1DRotationConstraintAsync(
                this.worker, this.worldUpVector, this.modelUpVector, this.modelCenter);
            NotificationHelper.SendInfo("Set plane constraint");
        }

        /// <summary>
        /// Limits the rotation of the model to the given plain.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        private void SetConstraintInTracker()
        {
            TrackingManager.CatchCommandErrors(SetConstraintInTrackerAsync(), this);
        }

        private async Task DisableConstraintInTrackerAsync()
        {
            await ModelTrackerCommands.DisableConstraintAsync(this.worker);
            NotificationHelper.SendInfo("Disabled plane constraint");
        }

        /// <summary>
        /// Disables the constraint in the current tracker.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        private void DisableConstraintInTracker()
        {
            TrackingManager.CatchCommandErrors(DisableConstraintInTrackerAsync(), this);
        }

        private void OnEnable()
        {
            if (this.trackingManager.GetTrackerInitialized())
            {
                SetConstraintInTracker();
            }
            TrackingManager.OnTrackerInitialized += SetConstraintInTracker;
        }

        private void OnDisable()
        {
            try
            {
                if (this.trackingManager.GetTrackerInitialized())
                {
                    DisableConstraintInTracker();
                }
                TrackingManager.OnTrackerInitialized -= SetConstraintInTracker;
            }
            catch (TrackingManagerNotFoundException)
            {
            }
        }
    }
}
