using System;
using System.Collections.Generic;
using Visometry.VisionLib.SDK.Core.API;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  Frame contains the description of one frame for tracking
    /// </summary>
    /// @ingroup Core
    [Serializable]
    public class Frame : InputFrame
    {
        public Image image;
        public IntrinsicData intrinsicData;
        public ExtrinsicData extrinsicData;
        public ExtrinsicData cameraTransform;
        public CalibratedImage calibratedDepthImage;
        public TrackingState trackingState;

        public Dictionary<string, SimilarityTransform> anchorTransforms =
            new Dictionary<string, SimilarityTransform>();
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Frame Evaluate()
        {
            var result = new Frame();
            result.intrinsicData = this.intrinsicData;
            result.extrinsicData = this.extrinsicData;
            result.cameraTransform = this.cameraTransform;
            result.anchorTransforms = this.anchorTransforms;
            result.image = this.image;
            result.calibratedDepthImage = this.calibratedDepthImage;
            this.intrinsicData = null;
            this.extrinsicData = null;
            this.cameraTransform = null;
            this.anchorTransforms = new Dictionary<string, SimilarityTransform>();
            this.image = null;
            this.calibratedDepthImage = null;
            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.image != null)
                {
                    this.image.Dispose();
                }
                if (this.intrinsicData != null)
                {
                    this.intrinsicData.Dispose();
                }
                if (this.extrinsicData != null)
                {
                    this.extrinsicData.Dispose();
                }
                if (this.cameraTransform != null)
                {
                    this.cameraTransform.Dispose();
                }
                if (this.calibratedDepthImage != null)
                {
                    this.calibratedDepthImage.Dispose();
                }
                foreach (var item in this.anchorTransforms)
                {
                    item.Value.Dispose();
                }
            }
            this.disposed = true;
        }

        public Frame Clone()
        {
            var result = new Frame();
            result.image = this.image?.Clone();
            result.intrinsicData = this.intrinsicData?.Clone();
            result.extrinsicData = this.extrinsicData?.Clone();
            result.cameraTransform = this.cameraTransform?.Clone();
            result.calibratedDepthImage = this.calibratedDepthImage?.Clone();
            foreach (var item in this.anchorTransforms)
            {
                result.anchorTransforms[item.Key] = item.Value.Clone();
            }
            result.trackingState = this.trackingState;
            return result;
        }
    }
}