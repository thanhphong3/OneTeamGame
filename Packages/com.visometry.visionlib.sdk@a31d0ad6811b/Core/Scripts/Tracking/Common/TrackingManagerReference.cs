using System;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  Base class for MonoBehaviours, which need access to the
    ///  <see cref="Worker"/> and <see cref="TrackingManager"/> objects.
    /// </summary>
    /// @ingroup Core
    public abstract class TrackingManagerReference : MonoBehaviour
    {
        private TrackingManager trackingManagerValue;

        private Worker workerValue;

        public class TrackingManagerNotFoundException : Exception
        {
            /// <summary>
            /// Exception that is thrown when the TrackingManager is tried to be accessed
            /// while it is null. This happens if there is no active GameObject
            /// with a `TrackingManager` component in the scene.
            /// </summary>
            public TrackingManagerNotFoundException() :
                base("Could not find a TrackingManager in the scene")
            {
            }
        }

        /// <summary>
        ///  Reference to used TrackingManager.
        /// </summary>
        /// <remarks>
        ///  Is set automatically by searching for an active GameObject
        ///  with a TrackingManager in the scene.
        /// </remarks>
        protected TrackingManager trackingManager
        {
            get
            {
                if (!FindTrackingManagerReference())
                {
                    throw new TrackingManagerNotFoundException();
                }
                return trackingManagerValue;
            }
        }

        protected Worker worker
        {
            get
            {
                this.workerValue = this.trackingManager.GetWorker();
                if (this.workerValue == null)
                {
                    throw new TrackingManager.WorkerNotFoundException();
                }
                return workerValue;
            }
        }

        protected virtual void ResetReference()
        {
            this.workerValue = null;
            this.trackingManagerValue = null;
        }

        /// <summary>
        ///  Initializes the <see cref="trackingManager"/> and <see cref="worker"/>
        ///  member variables.
        /// </summary>
        /// <returns>
        ///  <c>true</c>, on success; <c>false</c> otherwise.
        /// </returns>
        [System.Obsolete(
            "InitWorkerReference is obsolete.\nThe reference will be searched automatically when accessing the \"trackingManager\" property.")]
        protected virtual bool InitWorkerReference()
        {
            return this.trackingManager != null && this.workerValue != null;
        }

        private bool FindTrackingManagerReference()
        {
            // TrackingManager already found?
            if (this.trackingManagerValue != null)
            {
                return true;
            }

            // Look for it at the same GameObject first
            this.trackingManagerValue = GetComponent<TrackingManager>();
            if (this.trackingManagerValue != null)
            {
                return true;
            }

            // Try to find it anywhere in the scene
            this.trackingManagerValue = FindObjectOfType<TrackingManager>();

            return this.trackingManagerValue;
        }
    }
}
