using UnityEngine;
using System;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/Screen Orientation Observer")]
    public class ScreenOrientationObserver : TrackingManagerReference
    {
        [Serializable]
        public class Overwrite
        {
            public enum Orientation { Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight }

            public bool active = false;
            public Orientation orientation = Orientation.Portrait;
        }

        private DeviceOrientation currentDeviceOrientation = DeviceOrientation.Unknown;

        public delegate void OrientationChangeAction(ScreenOrientation orientation);
        public static event OrientationChangeAction OnOrientationChange;

        public delegate void SizeChangeAction(int width, int height);
        public static event SizeChangeAction OnSizeChange;

        /// <summary>
        ///  Settings for overwriting the screen orientation.
        /// </summary>
        /// <remarks>
        ///  On systems without a screen orientation sensor, Unity will always
        ///  report a portrait screen orientation. By activating the orientation
        ///  overwrite, it's possible to simulate a different screen orientation.
        ///  This allows the proper playback of iOS and Android image sequences
        ///  captured in landscape mode with an "imageRecorder" configuration.
        /// </remarks>
        public Overwrite overwrite;

        private ScreenOrientation orientation = ScreenOrientation.Portrait;
        private int width = -1;
        private int height = -1;

        private static ScreenOrientationObserver instance = null;
        private static ScreenOrientationObserver Instance
        {
            get
            {
                if (ScreenOrientationObserver.instance == null)
                {
                    ScreenOrientationObserver.instance =
                        FindObjectOfType<ScreenOrientationObserver>();
                }
                return ScreenOrientationObserver.instance;
            }
        }

        [System.Obsolete(
            "FindInstance(GameObject go) is obsolete. Please use FindInstance() instead.")]
        public static ScreenOrientationObserver FindInstance(GameObject go)
        {
            return FindInstance();
        }

        public static ScreenOrientationObserver FindInstance()
        {
            return ScreenOrientationObserver.Instance;
        }

        [System.Obsolete(
            "GetOrientation(GameObject go) is obsolete. Please use GetScreenOrientation() instead.")]
        public static ScreenOrientation GetOrientation(GameObject go)
        {
            return ScreenOrientationObserver.GetScreenOrientation();
        }

        public static ScreenOrientation GetScreenOrientation()
        {
            if (ScreenOrientationObserver.Instance == null)
            {
#if !UNITY_WSA_10_0
                LogHelper.LogWarning(
                    "No ScreenOrientationObserver component found in scene. Returning default orientation.");
#endif
#if (UNITY_WSA_10_0 || UNITY_ANDROID || UNITY_IOS)
                return ScreenOrientation.LandscapeLeft;
#else
                return ScreenOrientation.Portrait;
#endif
            }

            return ScreenOrientationObserver.Instance.GetOrientation();
        }

        /// <summary>
        ///  Returns the current screen orientation considering the overwrite
        ///  setting.
        /// </summary>
        /// <returns>
        ///  Screen.orientation or <see cref="overwrite.orientation"/> depending on
        ///  the <see cref="overwrite.active"/> value.
        /// </returns>
        public ScreenOrientation GetOrientation()
        {
            if (!this.overwrite.active)
            {
                return Screen.orientation;
            }

            // Use the user-defined screen orientation
            switch (this.overwrite.orientation)
            {
                case Overwrite.Orientation.Portrait:
                    return ScreenOrientation.Portrait;
                case Overwrite.Orientation.PortraitUpsideDown:
                    return ScreenOrientation.PortraitUpsideDown;
                case Overwrite.Orientation.LandscapeLeft:
                    return ScreenOrientation.LandscapeLeft;
                case Overwrite.Orientation.LandscapeRight:
                    return ScreenOrientation.LandscapeRight;
            }

            // This should never happen
            return ScreenOrientation.AutoRotation;
        }

        private void Awake()
        {
            if (ScreenOrientationObserver.instance != null &&
                ScreenOrientationObserver.instance != this)
            {
                Debug.LogWarning(
                    "There already is another ScreenOrientationObserver(" +
                        ScreenOrientationObserver.instance.gameObject.name +
                        ") in the Scene. Please make sure that there is only one active ScreenOrientationObserver.",
                    ScreenOrientationObserver.instance);
                return;
            }
            ScreenOrientationObserver.instance = this;
        }

        private void OnEnable()
        {
            UpdateScreenOrientation();
            UpdateScreenSize();
            UpdateDeviceOrientation();
        }

        private void OnDestroy()
        {
            ScreenOrientationObserver.instance = null;
        }

        private void Update()
        {
            UpdateScreenOrientation();
            UpdateScreenSize();
            UpdateDeviceOrientation();
        }

        private void UpdateScreenOrientation()
        {
            // Get the screen orientation from Unity
            ScreenOrientation currentOrientation = this.GetOrientation();

            // Orientation not changed?
            if (currentOrientation == this.orientation)
            {
                return;
            }

            // The screen orientation should never be 'AutoRotation'
            if (currentOrientation == ScreenOrientation.AutoRotation)
            {
                LogHelper.LogWarning("Cannot derive correct screen orientation");
                return;
            }

            // Unity sometimes returns an unknown screen orientation on iOS for some reason.
            // Therefore we check if the value is valid here.
            if (currentOrientation == ScreenOrientation.Portrait ||
                currentOrientation == ScreenOrientation.PortraitUpsideDown ||
                currentOrientation == ScreenOrientation.LandscapeLeft ||
                currentOrientation == ScreenOrientation.LandscapeRight)
            {
                this.orientation = currentOrientation;

                // Emit change event
                OnOrientationChange?.Invoke(this.orientation);
            }
        }

        private void UpdateScreenSize()
        {
            // Device orientation changed?
            if (Screen.width != this.width || Screen.height != this.height)
            {
                this.width = Screen.width;
                this.height = Screen.height;

                OnSizeChange?.Invoke(this.width, this.height);
            }
        }

        private void UpdateDeviceOrientation()
        {
            if (this.currentDeviceOrientation != Input.deviceOrientation)
            {
                this.currentDeviceOrientation = Input.deviceOrientation;
                SetDeviceOrientation(this.currentDeviceOrientation);
            }
        }

        private async Task SetDeviceOrientationAsync(DeviceOrientation devOrientation)
        {
            // default image orientation mode in vlSDK
            int imageRotationMode = 0;

            switch (devOrientation)
            {
                case DeviceOrientation.LandscapeRight:
                    imageRotationMode = 2;
                    break;
                case DeviceOrientation.LandscapeLeft:
                    imageRotationMode = 0;
                    break;
                case DeviceOrientation.Portrait:
                    imageRotationMode = 3;
                    break;
                case DeviceOrientation.PortraitUpsideDown:
                    imageRotationMode = 1;
                    break;
            }

            try
            {
                await WorkerCommands.SetDeviceOrientationAsync(this.worker, imageRotationMode);
            }
            catch (TrackingManager.WorkerNotFoundException)
            {
            }
        }

        /// <summary>
        /// Sets the image rotation mode of AutoInitManager in vlSDK
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="devOrientation"></param>
        private void SetDeviceOrientation(DeviceOrientation devOrientation)
        {
            TrackingManager.CatchCommandErrors(SetDeviceOrientationAsync(devOrientation), this);
        }
    }
}
