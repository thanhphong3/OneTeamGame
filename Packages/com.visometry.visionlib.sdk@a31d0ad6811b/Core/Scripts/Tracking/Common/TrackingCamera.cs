using UnityEngine;
using System;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  Camera used for rendering the augmentation.
    /// </summary>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Tracking Camera")]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(Transform))]
    public class TrackingCamera : MonoBehaviour
    {
        public enum CoordinateSystemAdjustment {
            SingleModelTracking,
            MultiModelTracking,
            Injection
        }

        public CoordinateSystemAdjustment coordinateSystemAdjustment =
            CoordinateSystemAdjustment.SingleModelTracking;

        /// <summary>
        ///  Layer with the camera image background.
        /// </summary>
        /// <remarks>
        ///  This layer will not be rendered by the tracking camera.
        /// </remarks>
        public int backgroundLayer = 8;

        private Camera trackingCamera;

        private Matrix4x4 renderRotationMatrixFromVLToUnity = CameraHelper.rotationZ0;
        private RenderRotation renderRotation = RenderRotation.CCW0;

        private float[] projectionMatrixArray = new float[16];
        private Matrix4x4 projectionMatrix = new Matrix4x4();

        private void OnCameraTransform(ExtrinsicData extrinsicData)
        {
            if (this.coordinateSystemAdjustment == CoordinateSystemAdjustment.MultiModelTracking)
            {
                Matrix4x4 modelViewMatrix = CameraHelper.flipYZ *
                                            extrinsicData.GetModelViewMatrix().inverse *
                                            CameraHelper.flipYZ;
                modelViewMatrix = modelViewMatrix * CameraHelper.flipXY;
                SetModelViewMatrix(modelViewMatrix);
            }
        }

        private void OnExtrinsicData(ExtrinsicData extrinsicData)
        {
            if (this.coordinateSystemAdjustment == CoordinateSystemAdjustment.SingleModelTracking)
            {
                SetModelViewMatrix(extrinsicData.GetModelViewMatrix());
            }
        }

        private void SetModelViewMatrix(Matrix4x4 modelViewMatrix)
        {
            try
            {
                Vector3 position;
                Quaternion rotation;
                CameraHelper.ModelViewMatrixToUnityPose(
                    modelViewMatrix,
                    this.renderRotationMatrixFromVLToUnity,
                    out position,
                    out rotation);
                this.trackingCamera.transform.rotation = rotation;
                this.trackingCamera.transform.position = position;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void OnIntrinsicData(IntrinsicData intrinsicData)
        {
            // Apply the intrinsic camera parameters
            if (intrinsicData.GetProjectionMatrix(
                    this.trackingCamera.nearClipPlane,
                    this.trackingCamera.farClipPlane,
                    Screen.width,
                    Screen.height,
                    renderRotation,
                    0,
                    projectionMatrixArray))
            {
                for (int i = 0; i < 16; ++i)
                {
                    projectionMatrix[i % 4, i / 4] = projectionMatrixArray[i];
                }
                this.trackingCamera.projectionMatrix = projectionMatrix;
            }
        }

        private void OnOrientationChange(ScreenOrientation orientation)
        {
            this.renderRotation = CameraHelper.GetRenderRotation(orientation);
            this.renderRotationMatrixFromVLToUnity =
                CameraHelper.GetRenderRotationMatrixFromVLToUnity(this.renderRotation);
        }

        private void Awake()
        {
            this.trackingCamera = this.GetComponent<Camera>();

            // Don't clear the background image
            this.trackingCamera.clearFlags = CameraClearFlags.Depth;

            // Render after the background camera
            this.trackingCamera.depth = 2;

            // Don't render the background image
            int mask = 1 << this.backgroundLayer;
            this.trackingCamera.cullingMask &= ~mask;
        }

        private void OnEnable()
        {
            OnOrientationChange(ScreenOrientationObserver.GetScreenOrientation());
            ScreenOrientationObserver.OnOrientationChange += OnOrientationChange;

            TrackingManager.OnExtrinsicData += OnExtrinsicData;
            TrackingManager.OnCameraTransform += OnCameraTransform;
            TrackingManager.OnIntrinsicData += OnIntrinsicData;
        }

        private void OnDisable()
        {
            TrackingManager.OnIntrinsicData -= OnIntrinsicData;
            TrackingManager.OnCameraTransform -= OnCameraTransform;
            TrackingManager.OnExtrinsicData -= OnExtrinsicData;
            ScreenOrientationObserver.OnOrientationChange -= OnOrientationChange;
        }
    }
}
