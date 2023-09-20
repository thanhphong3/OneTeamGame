using UnityEngine;
using System;
using UnityEngine.Serialization;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  Camera used to define the initial pose.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   It's possible to change the camera position and orientation at
    ///   runtime. The new initial pose will then be used while the tracking is
    ///   lost.
    ///  </para>
    ///  <para>
    ///   If there is no InitCamera in the scene or the
    ///   InitCamera is disabled, then the initial pose from the
    ///   tracking configuration file will be used.
    ///  </para>
    ///  <para>
    ///   Please make sure, that there is only one active InitCamera in
    ///   the scene. Otherwise both components will try to set the initial pose,
    ///   which will lead to unexpected behaviour.
    ///  </para>
    ///  <para>
    ///   Right now this behaviour does not work with the HoloLens model-based
    ///   tracking. In that case please use the HoloLensInitCamera
    ///   or VLHoloLensInitCamera prefab instead.
    ///  </para>
    /// </remarks>
    /// @ingroup Core
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("VisionLib/Core/Init Camera")]
    public class InitCamera : TrackingManagerReference
    {
        /// <summary>
        ///  Reference to the Camera component attached to the GameObject.
        /// </summary>
        private Camera initCamera;

        /// <summary>
        ///  Layer with the camera image background.
        /// </summary>
        /// <remarks>
        ///  This layer will not be rendered.
        /// </remarks>
        public int backgroundLayer = 8;

        /// <summary>
        ///  Use the last valid camera pose as initialization pose.
        /// </summary>
        /// <remarks>
        ///  Since this might results in an awkward InitCamera transformation, it's
        ///  recommended to give the user the option to restore the original pose
        ///  using the <see cref="ResetToOriginalPose"/> function.
        /// </remarks>
        [Tooltip("Use the last valid pose as initialization pose")]
        public bool useLastValidPose;

        /// <summary>
        ///  Overwrite init camera transformation with values from tracking
        ///  configuration on tracking start.
        /// </summary>
        /// <remarks>
        ///  The InitCamera can then be transformed afterwards, but will get
        ///  overwritten again after loading a new tracking configuration.
        /// </remarks>
        [Tooltip(
            "Overwrite init camera transformation with values from tracking configuration on tracking start")]
        [FormerlySerializedAs("overwriteOnLoad")]
        public bool usePoseFromTrackingConfig;

        private Matrix4x4 renderRotationMatrixFromUnityToVL = CameraHelper.rotationZ0;
        private Matrix4x4 renderRotationMatrixFromVLToUnity = CameraHelper.rotationZ0;
        private RenderRotation renderRotation = RenderRotation.CCW0;

        private float[] projectionMatrixArray = new float[16];
        private Matrix4x4 projectionMatrix = new Matrix4x4();

        private bool settingInitPose;
        private bool gotInitPose = false;
        private bool resetToOriginalPose;

        private Vector3 originalPosition;
        private Quaternion originalOrientation;

        private TransformCache initPoseInBackend;

        [Serializable]
        private class InitPose
        {
            public string type;
            public float[] t = new float[3];
            public float[] q = new float[4];

            public InitPose(Vector4 t, Quaternion q)
            {
                this.type = "visionlib";
                Set(t, q);
            }

            public void Set(Vector4 t, Quaternion q)
            {
                this.t[0] = t.x;
                this.t[1] = t.y;
                this.t[2] = t.z;

                this.q[0] = q.x;
                this.q[1] = q.y;
                this.q[2] = q.z;
                this.q[3] = q.w;
            }
        }

        /// <summary>
        ///  Restores the original transformation of the InitCamera.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   This might be useful if the InitCamera was transformed in some
        ///   awkward way for some reason (e.g. because
        ///   <see cref="useLastValidPose"/> is set to <c>true</c> and the tracking
        ///   failed) and we quickly want to restore the original state.
        ///  </para>
        ///  <para>
        ///   If <see cref="usePoseFromTrackingConfig"/> is set to <c>false</c>, then this
        ///   will restore the transformation during the initialization of the
        ///   InitCamera. If <see cref="usePoseFromTrackingConfig"/> is set to
        ///   <c>true</c>, then this will restore the transformation from the
        ///   tracking configuration.
        ///  </para>
        /// </remarks>
        public void ResetToOriginalPose()
        {
            this.resetToOriginalPose = true;
        }

        [System.Obsolete(
            "The `void Reset()` function of InitCamera is obsolete. Use the ResetToOriginalPose functions instead.")]
        public void Reset()
        {
            ResetToOriginalPose();
        }

        private static ModelTrackerCommands.InitPose
            CameraToInitParam(Camera camera, Matrix4x4 offset)
        {
            Vector4 t;
            Quaternion q;
            CameraHelper.CameraToVLPose(camera, offset, out t, out q);
            return new ModelTrackerCommands.InitPose(t, q);
        }

        private void OnOrientationChange(ScreenOrientation orientation)
        {
            this.initPoseInBackend.Invalidate();
            this.renderRotation = CameraHelper.GetRenderRotation(orientation);
            this.renderRotationMatrixFromVLToUnity =
                CameraHelper.GetRenderRotationMatrixFromVLToUnity(this.renderRotation);
            this.renderRotationMatrixFromUnityToVL =
                CameraHelper.GetRenderRotationMatrixFromUnityToVL(this.renderRotation);
        }

        private void OnTrackerInitializing()
        {
            this.settingInitPose = false;
            this.resetToOriginalPose = false;
        }

        private void OnTrackerInitialized()
        {
            this.initPoseInBackend.Invalidate();
            if (!this.usePoseFromTrackingConfig)
            {
                this.UpdateInitPoseIfTransformChanged();
                this.gotInitPose = true;
            }
            else
            {
                this.GetInitPose();
            }
        }

        private void OnExtrinsicData(ExtrinsicData extrinsicData)
        {
            if (!this.useLastValidPose || !this.gotInitPose || !extrinsicData.GetValid())
            {
                return;
            }

            try
            {
                Vector3 position;
                Quaternion rotation;
                CameraHelper.ModelViewMatrixToUnityPose(
                    extrinsicData.GetModelViewMatrix(),
                    this.renderRotationMatrixFromVLToUnity,
                    out position,
                    out rotation);
                this.initCamera.transform.rotation = rotation;
                this.initCamera.transform.position = position;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void OnIntrinsicData(IntrinsicData intrinsicData)
        {
            // Apply the intrinsic camera parameters
            if (intrinsicData.GetProjectionMatrix(
                    this.initCamera.nearClipPlane,
                    this.initCamera.farClipPlane,
                    Screen.width,
                    Screen.height,
                    this.renderRotation,
                    0,
                    projectionMatrixArray))
            {
                for (int i = 0; i < 16; ++i)
                {
                    projectionMatrix[i % 4, i / 4] = projectionMatrixArray[i];
                }
                this.initCamera.projectionMatrix = projectionMatrix;
            }
        }

        private async Task GetInitPoseAsync()
        {
            this.gotInitPose = false;

            var result = await ModelTrackerCommands.GetInitPoseAsync(this.worker);

            Vector3 position;
            Quaternion orientation;
            CameraHelper.VLPoseToCamera(
                new Vector3(result.t[0], result.t[1], result.t[2]),
                new Quaternion(result.q[0], result.q[1], result.q[2], result.q[3]),
                out position,
                out orientation);

            this.initCamera.transform.position = position;
            this.initCamera.transform.rotation = orientation;
            SetOriginalPose(position, orientation);
            this.gotInitPose = true;
        }

        /// <summary>
        /// Receives the current InitPose from VisionLib and sets it internally.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        private void GetInitPose()
        {
            TrackingManager.CatchCommandErrors(GetInitPoseAsync(), this);
        }

        private async Task SetInitPoseAsync()
        {
            this.settingInitPose = true;
            ModelTrackerCommands.InitPose initPose =
                CameraToInitParam(this.initCamera, this.renderRotationMatrixFromUnityToVL);

            await ModelTrackerCommands.SetInitPoseAsync(this.worker, initPose);

            this.settingInitPose = false;
        }

        /// <summary>
        /// Sets the internal InitPose inside VisionLib.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        private void SetInitPose()
        {
            TrackingManager.CatchCommandErrors(SetInitPoseAsync(), this);
        }

        private void UpdateInitPoseIfTransformChanged()
        {
            this.initPoseInBackend.Write(this.initCamera.transform);
        }

        private void Initialize()
        {
            if (this.initPoseInBackend != null)
            {
                return;
            }

            this.initPoseInBackend = new TransformCache(SetInitPose);
        }

        private void Awake()
        {
            Initialize();
            this.initCamera = GetComponent<Camera>();

            // Store the original transformation so we can restore it later
            SetOriginalPose(this.initCamera.transform.position, this.initCamera.transform.rotation);
        }

        private void OnEnable()
        {
            Initialize();
            OnOrientationChange(ScreenOrientationObserver.GetScreenOrientation());
            ScreenOrientationObserver.OnOrientationChange += OnOrientationChange;

            TrackingManager.OnExtrinsicData += OnExtrinsicData;
            TrackingManager.OnIntrinsicData += OnIntrinsicData;
            TrackingManager.OnTrackerInitializing += OnTrackerInitializing;
            TrackingManager.OnTrackerInitialized += OnTrackerInitialized;

            if (this.trackingManager.GetTrackerInitialized())
            {
                OnTrackerInitialized();
            }
        }

        private void OnDisable()
        {
            TrackingManager.OnTrackerInitialized -= OnTrackerInitialized;
            TrackingManager.OnTrackerInitializing -= OnTrackerInitializing;
            TrackingManager.OnIntrinsicData -= OnIntrinsicData;
            TrackingManager.OnExtrinsicData -= OnExtrinsicData;

            ScreenOrientationObserver.OnOrientationChange -= OnOrientationChange;
        }

        private void Update()
        {
            if (!this.trackingManager.GetTrackerInitialized())
            {
                return;
            }

            if (this.resetToOriginalPose && this.gotInitPose)
            {
                this.initCamera.transform.position = this.originalPosition;
                this.initCamera.transform.rotation = this.originalOrientation;
                this.resetToOriginalPose = false;
            }

            if (!this.settingInitPose && this.gotInitPose)
            {
                this.UpdateInitPoseIfTransformChanged();
            }
        }

        /// <summary>
        /// Get the init pose values which are calculated using
        /// the init camera set in the InitCamera.
        /// </summary>
        /// <remarks>
        /// This way the init pose can be re-used
        /// e.g. in the tracking configuration.
        /// </remarks>
        /// <returns>Init pose values as string in json format</returns>
        public string GetInitPoseAsString()
        {
            Camera cam = this.initCamera ? this.initCamera : GetComponent<Camera>();
            Vector4 t;
            Quaternion q;

            // Get the VisionLib transformation from the Unity camera
            CameraHelper.CameraToVLPose(cam, this.renderRotationMatrixFromUnityToVL, out t, out q);

            // Convert the transformation into JSON
            InitPose initPose = new InitPose(t, q);

            return JsonHelper.ToJson(initPose, true);
        }

        /// <summary>
        /// Set the original init camera position and rotation.
        /// </summary>
        /// <remarks>
        /// The init cameras transform will be reset to this pose when InitCamera.Reset()
        /// is called
        /// </remarks>
        private void SetOriginalPose(Vector3 position, Quaternion orientation)
        {
            this.originalPosition = position;
            this.originalOrientation = orientation;
        }
    }
}
