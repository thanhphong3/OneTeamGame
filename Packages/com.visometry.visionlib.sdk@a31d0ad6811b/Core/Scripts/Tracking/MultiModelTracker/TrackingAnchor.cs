using System;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    /// Tracking anchor that is used to control references like the
    /// visualization model and WorkSpaces for different anchors
    /// in multi model tracking.
    /// **THIS IS SUBJECT TO CHANGE** Please do not rely on this code in productive environments.
    /// </summary>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Tracking Anchor")]
    public class TrackingAnchor : TrackingManagerReference
    {
        /// <summary>
        /// Unique name that represents the tracking anchors name in the vl file.
        /// </summary>
        [Tooltip("Unique name that represents the tracking anchors name in the vl file.")]
        public string anchorName;

        /// <summary>
        /// Reference to the model that should be used for augmenting the tracking target.
        /// </summary>
        [Tooltip("Reference to the model that should be used for augmenting the tracking target.")]
        public GameObject augmentation;

        /// <summary>
        /// List of WorkSpaces that are used to initialize the tracking target.
        /// </summary>
        [Tooltip("List of WorkSpaces that are used to initialize the tracking target.")]
        public WorkSpace[] workSpaces;

        /// <summary>
        ///  Interpolation time to apply updates to the augmentation Transform.
        /// </summary>
        /// <remarks>
        ///  Set to 0 to directly apply tracking results without smoothing.
        /// </remarks>
        [Tooltip("Interpolation time to apply updates to the augmentation Transform.")]
        public float smoothTime = 0f;

        private PositionUpdateDamper interpolationTarget = new PositionUpdateDamper();

        private bool isAnchorEnabled = false;
        private bool isAnchorRegistered = false;
        private bool augmentationIsActive = true;
        private Renderer[] rendererList;
        private Camera lastSetInitPoseCamera = null;
        private ModelTransform? lastSetInitPose = null;
        private bool setGlobalInitPoseCalled = false;

        private void Start()
        {
            if (this.augmentation == null)
            {
                LogHelper.LogError(
                    "Augmentation is null. Did you forget to set the 'augmentation' property?",
                    this);
                return;
            }

            if (this.anchorName == "")
            {
                LogHelper.LogError(
                    "AnchorName is empty. Did you forget to set the 'anchorName' property?", this);
                return;
            }

            this.rendererList = this.augmentation.GetComponentsInChildren<Renderer>();
            this.augmentationIsActive = GetAugmentationActive();

            if (IsAnchorConfigured())
            {
                EnableAnchorOnTrackerInitialized();
            }
        }

        private async Task EnableAnchorAsync()
        {
            if (this.isAnchorEnabled)
            {
                return;
            }
            this.isAnchorEnabled = true;
            await MultiModelTrackerCommands.EnableAnchorAsync(this.worker, this.anchorName);
            NotificationHelper.SendInfo("Enabled Anchor " + this.anchorName);
        }

        /// <summary>
        /// Enables this anchor for tracking.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        private void EnableAnchor()
        {
            TrackingManager.OnTrackerInitialized -= EnableAnchor;
            TrackingManager.CatchCommandErrors(EnableAnchorAsync(), this);
        }

        private async Task DisableAnchorAsync()
        {
            if (!this.isAnchorEnabled)
            {
                return;
            }
            this.isAnchorEnabled = false;
            try
            {
                await MultiModelTrackerCommands.DisableAnchorAsync(this.worker, this.anchorName);
                NotificationHelper.SendInfo("Disabled Anchor " + this.anchorName);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Disables this anchor for tracking.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        private void DisableAnchor()
        {
            TrackingManager.OnTrackerInitialized -= DisableAnchor;
            TrackingManager.CatchCommandErrors(DisableAnchorAsync(), this);
        }

        public async Task AnchorResetHardAsync()
        {
            await MultiModelTrackerCommands.AnchorResetHardAsync(this.worker, this.anchorName);
            NotificationHelper.SendInfo("Anchor " + this.anchorName + " reset tracking");
        }

        /// <summary>
        /// Resets the tracking for this anchor.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void AnchorResetHardAnchor()
        {
            TrackingManager.CatchCommandErrors(AnchorResetHardAsync(), this);
        }

        public async Task AnchorAddModelAsync(string modelName, string modelURI, bool enabled, bool occluder)
        {
            ModelProperties modelProperties = new ModelProperties(modelName, modelURI, enabled, occluder);

            await MultiModelTrackerCommands.AnchorAddModelAsync(
                this.worker,
                this.anchorName,
                modelProperties);
            NotificationHelper.SendInfo(
                "Added Model " + modelProperties.name + " to Anchor " + this.anchorName);
        }

        public void AnchorAddModel(
            string modelName, string modelURI, bool enabled = true, bool occluder = false)
        {
            TrackingManager.CatchCommandErrors(
                AnchorAddModelAsync(modelName, modelURI, enabled, occluder), this);
        }

        public async Task AnchorSetModelPropertyEnabledAsync(string name, bool state)
        {
            await MultiModelTrackerCommands.AnchorSetModelPropertyEnabledAsync(
                this.worker,
                this.anchorName,
                name,
                state);
            NotificationHelper.SendInfo(
                "Set model property enabled to " + state + " in anchor " + this.anchorName);
        }

        /// <summary>
        /// Enables/Disables a specific model for this anchor
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="name">Name of the model</param>
        /// <param name="state">Enabled (true/false)</param>
        public void AnchorSetModelPropertyEnabled(string name, bool state)
        {
            TrackingManager.CatchCommandErrors(
                AnchorSetModelPropertyEnabledAsync(name, state), this);
        }

        public async Task AnchorSetModelPropertyOccluderAsync(string name, bool state)
        {
            await MultiModelTrackerCommands.AnchorSetModelPropertyOccluderAsync(
                this.worker,
                this.anchorName,
                name,
                state);
            NotificationHelper.SendInfo(
                "Set model property occluder to " + state + " in anchor " + this.anchorName);
        }

        /// <summary>
        /// Loads a specific model for this anchor, which is specified by an uri.
        /// All other models will be removed from this anchor.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="name">Name of the model</param>
        /// <param name="uri">Path to the model file
        public void AnchorSetModelPropertyURI(string name, string uri)
        {
            TrackingManager.CatchCommandErrors(
                AnchorSetModelPropertyURIAsync(name, uri), this);
        }

        public async Task AnchorSetModelPropertyURIAsync(string name, string uri)
        {
            await MultiModelTrackerCommands.AnchorSetModelPropertyURIAsync(
                this.worker,
                this.anchorName,
                name,
                uri);
            NotificationHelper.SendInfo(
                "Set model property uri to " + uri + " in anchor " + this.anchorName);
        }

        [System.Obsolete(
            "SetInitPoseAsync is obsolete. Please use SetGlobalInitPoseAsync instead.")]
        public async Task SetInitPoseAsync()
        {
            await SetGlobalInitPoseAsync();
        }

        [System.Obsolete("SetInitPose is obsolete. Please use SetGlobalInitPose instead.")]
        public void SetInitPose()
        {
            SetGlobalInitPose();
        }

        public async Task SetGlobalInitPoseAsync()
        {
            await MultiModelTrackerCommands.SetGlobalObjectPoseAsync(
                this.worker,
                this.anchorName,
                CameraHelper.TransformToGlobalObjectPose(this.augmentation.transform));
            this.setGlobalInitPoseCalled = true;
        }

        public void SetGlobalInitPose()
        {
            TrackingManager.CatchCommandErrors(SetGlobalInitPoseAsync(), this);
        }

        public async Task SetInitPoseRelativeToCameraAsync(Camera usedCamera)
        {
            var renderRotation =
                CameraHelper.GetRenderRotation(ScreenOrientationObserver.GetScreenOrientation());
            var renderRotationMatrixFromUnityToVL =
                CameraHelper.GetRenderRotationMatrixFromUnityToVL(renderRotation);
            var cameraFromWorldTransform = renderRotationMatrixFromUnityToVL *
                                           new ModelTransform(usedCamera.transform).Inverse();
            this.lastSetInitPose = CameraHelper.UnityToVLPose(
                cameraFromWorldTransform * new ModelTransform(this.augmentation.transform));
            this.lastSetInitPoseCamera = usedCamera;

            await ModelTrackerCommands.SetInitPoseAsync(
                this.worker, new ModelTrackerCommands.InitPose(this.lastSetInitPose.Value));
        }

        public void SetInitPoseRelativeToCamera(Camera usedCamera)
        {
            TrackingManager.CatchCommandErrors(SetInitPoseRelativeToCameraAsync(usedCamera), this);
        }

        public async Task<ModelTransform> GetInitPoseAsync()
        {
            var initPose = await MultiModelTrackerCommands.AnchorGetInitPoseAsync(
                this.worker, this.anchorName);

            return new ModelTransform(initPose);
        }

        /// <summary>
        /// Sets a specific model as occluder for this anchor
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="name">Name of the model</param>
        /// <param name="state">Occluder (true/false)</param>
        public void AnchorSetModelPropertyOccluder(string name, bool state)
        {
            TrackingManager.CatchCommandErrors(
                AnchorSetModelPropertyOccluderAsync(name, state), this);
        }

        public async Task AnchorSetAttributeAsync(string attributeName, string value)
        {
            await MultiModelTrackerCommands.AnchorSetAttributeAsync(
                this.worker,
                attributeName,
                new List<MultiModelTrackerCommands.AnchorAttribute>(){
                    new MultiModelTrackerCommands.AnchorAttribute(this.anchorName, value)});
            NotificationHelper.SendInfo(
                "Set attribute " + attributeName + " to value " + value + " in anchor " +
                this.anchorName);
        }

        /// <summary>
        /// Sets an attribute to the given value for this anchor
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="attributeName">Name of the attribute</param>
        /// <param name="value">New value for this attribute</param>
        public void AnchorSetAttribute(string attributeName, string value)
        {
            TrackingManager.CatchCommandErrors(AnchorSetAttributeAsync(attributeName, value), this);
        }

        private void OnEnable()
        {
            if (IsAnchorConfigured())
            {
                EnableAnchorOnTrackerInitialized();
            }
        }

        private void OnDisable()
        {
            if (IsAnchorConfigured())
            {
                DisableAnchorOnTrackerInitialized();
            }
        }

        private void OnDestroy()
        {
            TrackingManager.OnTrackerInitialized -= EnableAnchor;
            TrackingManager.OnTrackerInitialized -= DisableAnchor;
        }

        private void Update()
        {
            if (this.augmentation == null)
            {
                return;
            }

            UpdateAugmentationTransform();
        }

        private void SetAugmentationActive(bool active)
        {
            if (this.augmentationIsActive == active)
            {
                return;
            }
            this.augmentationIsActive = active;

            foreach (var renderer in this.rendererList)
            {
                renderer.enabled = active;
            }
        }

        private bool IsAnchorConfigured()
        {
            return this.augmentation != null && this.anchorName != "";
        }

        private bool GetAugmentationActive()
        {
            return this.rendererList.Any(renderer => renderer.enabled);
        }

        private bool ContainsOutdatedInitPose(SimilarityTransform modelToWorldST)
        {
            bool updateFromBeforeSetGlobalInitPose =
                this.setGlobalInitPoseCalled && !modelToWorldST.GetValid();
            if (updateFromBeforeSetGlobalInitPose)
            {
                this.setGlobalInitPoseCalled = false;
                return true;
            }
            bool notCalledSetInitPoseRecently =
                this.lastSetInitPose == null || this.lastSetInitPoseCamera == null;
            if (notCalledSetInitPoseRecently || modelToWorldST.GetValid())
            {
                return false;
            }
            Vector4 t;
            Quaternion q;
            CameraHelper.WorldToCameraMatrixToVLPose(
                this.lastSetInitPoseCamera.worldToCameraMatrix * CameraHelper.flipXY, out t, out q);
            var worldToCameraTransform = new ModelTransform(q, t);
            var modelToWorldTransform = new ModelTransform(modelToWorldST);
            var modelToCameraTransform = worldToCameraTransform * modelToWorldTransform;
            return modelToCameraTransform != this.lastSetInitPose.Value;
        }

        private void OnSimilarityTransform(SimilarityTransform similarityTransformWrapper)
        {
            var mt = new ModelTransform(similarityTransformWrapper);
            bool trackingIsInvalidAndNoInitPoseIsSet = mt.IsFarAway();

            SetAugmentationActive(!trackingIsInvalidAndNoInitPoseIsSet);

            if (trackingIsInvalidAndNoInitPoseIsSet)
            {
                this.interpolationTarget.Invalidate();
                return;
            }

            if (ContainsOutdatedInitPose(similarityTransformWrapper))
            {
                this.interpolationTarget.Invalidate();
                return;
            }
            this.lastSetInitPose = null;
            this.lastSetInitPoseCamera = null;

            var mtUnityWorld = CameraHelper.flipXY * mt;
            this.interpolationTarget.SetData(mtUnityWorld);
            UpdateAugmentationTransform();
        }

        private void UpdateAugmentationTransform()
        {
            this.interpolationTarget.Slerp(this.smoothTime, this.augmentation);
        }

        private void EnableAnchorOnTrackerInitialized()
        {
            if (this.trackingManager.GetTrackerInitialized())
            {
                EnableAnchor();
            }
            else
            {
                TrackingManager.OnTrackerInitialized += EnableAnchor;
            }

            if (!this.isAnchorRegistered)
            {
                TrackingManager.AnchorTransform(this.anchorName).OnUpdate += OnSimilarityTransform;
                this.isAnchorRegistered = true;
            }
        }

        private void DisableAnchorOnTrackerInitialized()
        {
            try
            {
                if (this.trackingManager.GetTrackerInitialized())
                {
                    DisableAnchor();
                }
                else
                {
                    TrackingManager.OnTrackerInitialized += DisableAnchor;
                }
            }
            catch (TrackingManagerNotFoundException)
            {
            }

            if (this.isAnchorRegistered)
            {
                TrackingManager.AnchorTransform(this.anchorName).OnUpdate -= OnSimilarityTransform;
                this.interpolationTarget.Invalidate();
                this.isAnchorRegistered = false;
            }
        }
    }
}
