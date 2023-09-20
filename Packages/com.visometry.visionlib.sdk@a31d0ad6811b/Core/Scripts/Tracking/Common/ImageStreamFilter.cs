using UnityEngine;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/Image Stream Filter")]
    public class ImageStreamFilter : TrackingManagerReference
    {
        /// <summary>
        /// Image stream of the image to display.
        /// </summary>
        [Tooltip("Image stream of the image to display")]
        public TrackingManager.ImageStream imageStream = TrackingManager.ImageStream.CameraImage;

        protected Texture2D texture;

        public void UseDebugImageStream()
        {
            this.imageStream = TrackingManager.ImageStream.DebugImage;
        }

        public void UseCameraImageStream()
        {
            this.imageStream = TrackingManager.ImageStream.CameraImage;
        }

        public void UseDepthImageStream()
        {
            this.imageStream = TrackingManager.ImageStream.DepthImage;
        }

        public void UseNoImageStream()
        {
            this.imageStream = TrackingManager.ImageStream.None;
        }

        public Texture2D GetTexture()
        {
            UpdateImageStream();
            return this.texture;
        }

        private void UpdateImageStream()
        {
            if (this.trackingManager.GetTrackerInitialized())
            {
                this.texture = this.trackingManager.GetStreamTexture(this.imageStream);
            }
            else
            {
                this.texture = Texture2D.blackTexture;
            }
        }
    }
}
