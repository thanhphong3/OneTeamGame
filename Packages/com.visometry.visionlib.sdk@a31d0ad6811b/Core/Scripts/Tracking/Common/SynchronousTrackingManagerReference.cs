namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    public abstract class SynchronousTrackingManagerReference : TrackingManagerReference
    {
        private SynchronousTrackingManager syncTrackingManagerValue = null;

        protected SynchronousTrackingManager syncTrackingManager
        {
            get
            {
                this.syncTrackingManagerValue = (SynchronousTrackingManager) this.trackingManager;

                if (this.syncTrackingManagerValue == null)
                {
                    throw new TrackingManagerNotFoundException();
                }
                return this.syncTrackingManagerValue;
            }
        }

        protected override void ResetReference()
        {
            base.ResetReference();
            this.syncTrackingManagerValue = null;
        }

        [System.Obsolete(
            "InitWorkerReference is obsolete.\nThe reference will be searched automatically when accessing the \"syncTrackingManager\" property.")]
        protected override bool InitWorkerReference()
        {
            return this.syncTrackingManager != null;
        }
    }
}
