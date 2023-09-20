using System;
using System.Collections.Generic;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    /// Use this component to save a reference to the used tracking
    /// configuration (vl-file), license and calibration file
    /// and to start tracking with the options:
    /// auto start, input selection and external SLAM.
    /// </summary>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Tracking Configuration")]
    public class TrackingConfiguration : TrackingManagerReference
    {
        [System.Obsolete(
            "`TrackingConfiguration.path` is obsolete. " +
            "Use `TrackingConfiguration.configurationFileReference.uri` instead.")]
        [SerializeField]
        private string path = "";

        [System.Serializable]
        public class FilePathReference
        {
            public string uri;

            public enum FieldType { Object, URI }
            [SerializeField]
            private FieldType fieldType;
        }

        [FilePathReferenceField(
            "Tracking Configuration",
            ".vl",
            FilePathReferenceFieldAttribute.Mandatory.Yes,
            FilePathReferenceFieldAttribute.AllowProjectDir.No)]
        [SerializeField]
        public FilePathReference configurationFileReference = null;

        [FilePathReferenceField(
            "License",
            ".xml",
            FilePathReferenceFieldAttribute.Mandatory.No,
            FilePathReferenceFieldAttribute.AllowProjectDir.No)]
        [SerializeField]
        private FilePathReference licenseFileReference = null;

        [FilePathReferenceField(
            "Calibration",
            ".json",
            FilePathReferenceFieldAttribute.Mandatory.No,
            FilePathReferenceFieldAttribute.AllowProjectDir.Yes)]
        [SerializeField]
        private FilePathReference calibrationFileReference = null;

        [Tooltip("Automatically start tracking as soon as a TrackingManager is enabled.")]
        public bool autoStartTracking = false;

        [Tooltip(
            "Enable SLAM e.g. from ARCore/ ARKit or internal SLAM. Changes will take effect on the next tracking start.")]
        public bool extendTrackingWithSLAM = false;

        [Tooltip("Show available input sources to select which one is used on tracking start.")]
        public bool useInputSelection = false;
        [HideInInspector]
        public bool useResolutionSelection = false;
        [Tooltip("If true, the camera selection dialog will also show up on mobile devices.")]
        public bool showOnMobileDevices = false;

        private InputSourceSelection inputSelection;

        private void OnEnable()
        {
#pragma warning disable CS0618 // TrackingConfiguration.path is obsolete
            if (this.path != "")
            {
                SetConfigurationUriFromLegacyPathParameter();
            }
#pragma warning restore CS0618 // TrackingConfiguration.path is obsolete

            if (this.autoStartTracking)
            {
                try
                {
                    StartTracking();
                }
                catch (TrackingManager.WorkerNotFoundException)
                {
                    TrackingManager.OnWorkerCreated += StartTracking;
                }
            }
        }

        /// <summary>
        /// Start tracking using the tracking configuration, license,
        /// and calibration that are set in this component.
        /// </summary>
        public void StartTracking()
        {
            StartTracking(null);

            if (this.autoStartTracking)
            {
                TrackingManager.OnWorkerCreated -= StartTracking;
            }
        }

        /// <summary>
        /// Start tracking with arguments that are only applied for this tracking start.
        /// </summary>
        public void StartTracking(
            bool? extendTrackingWithSLAMOverride = null,
            bool? useInputSelectionOverride = null,
            bool? useResolutionSelectionOverride = null,
            bool? showOnMobileDevicesOverride = null)
        {
            SetLicenseAndCalibrationInTrackingManager();

            if ((useInputSelectionOverride ?? this.useInputSelection) &&
                ((showOnMobileDevicesOverride ?? this.showOnMobileDevices) || !RunsOnMobileDevice()))
            {
                StartCameraSelection(useResolutionSelectionOverride ?? this.useResolutionSelection);
                return;
            }

            StartTrackingWithParameters(
                extendTrackingWithSLAMOverride ?? this.extendTrackingWithSLAM);
            return;
        }

        private void SetLicenseAndCalibrationInTrackingManager()
        {
            if (!String.IsNullOrEmpty(this.licenseFileReference.uri))
            {
                this.trackingManager.licenseFile.path = this.licenseFileReference.uri;
            }

            if (!String.IsNullOrEmpty(this.calibrationFileReference.uri))
            {
                this.trackingManager.calibrationDataBaseURI = this.calibrationFileReference.uri;
            }
        }

        private void StartTrackingWithParameters(
            bool extendibleTracking,
            InputSourceSelection.InputSource inputSource = null)
        {
            if (String.IsNullOrEmpty(this.configurationFileReference.uri))
            {
                string errorMessage = "No tracking configuration set. Can not start tracking.";
                NotificationHelper.SendError(errorMessage, this);

                return;
            }

            List<string> additionalQueryParameters = GetAdditionalQueryParameters(
                this.configurationFileReference.uri, extendibleTracking, inputSource);
            string pathWithQuery = PathHelper.AppendQueryToURI(
                additionalQueryParameters, this.configurationFileReference.uri);

            LogHelper.LogDebug("Start Tracking with uri: " + pathWithQuery);
            this.trackingManager.StartTracking(pathWithQuery);
        }

        private void StartCameraSelection(bool resolutionSelection)
        {
            if (this.inputSelection == null)
            {
                this.inputSelection = this.gameObject.AddComponent<InputSourceSelection>();
            }
            this.inputSelection.OnInputSelected += StartTrackingWithInputParameters;

            DeviceInfo deviceInfo = this.trackingManager.GetDeviceInfo();
            if (deviceInfo != null)
            {
                this.inputSelection.StartInputSelection(deviceInfo, resolutionSelection);
            }
            else
            {
                LogHelper.LogError("An internal error occurred: Could not get device information.");
            }
        }

        private void StartTrackingWithInputParameters(InputSourceSelection.InputSource inputSource)
        {
            StartTrackingWithParameters(this.extendTrackingWithSLAM, inputSource);
            this.inputSelection.OnInputSelected -= StartTrackingWithInputParameters;
        }

        public void CancelCameraSelection()
        {
            if (this.inputSelection != null)
            {
                this.inputSelection.Cancel();
            }
        }

        private static bool RunsOnMobileDevice()
        {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            return true;
#else
            return false;
#endif
        }

        private static List<string> GetAdditionalQueryParameters(
            string uri,
            bool extendTracking,
            InputSourceSelection.InputSource inputSource)
        {
            List<string> queryParameters = new List<string>();
            Dictionary<string, string> queryMap = PathHelper.GetQueryMap(uri);

            if (extendTracking && !queryMap.ContainsKey("tracker.parameters.extendibleTracking"))
            {
                queryParameters.Add("tracker.parameters.extendibleTracking=true");
            }

            if (inputSource != null && !queryMap.ContainsKey("input.useDeviceID"))
            {
                queryParameters.Add(inputSource.GetQueryString());
            }

            return queryParameters;
        }

        private void SetConfigurationUriFromLegacyPathParameter()
        {
#pragma warning disable CS0618 // TrackingConfiguration.path is obsolete
            this.configurationFileReference.uri =
                PathHelper.CombinePaths("streaming-assets-dir:VisionLib", this.path);
            this.path = "";
#pragma warning restore CS0618 // TrackingConfiguration.path is obsolete
        }

        /// <summary>
        /// If SLAM (e.g. ARKit/ ARCore or internal SLAM) is enabled.
        /// Will restart the tracking if it is already running
        /// to apply the changes.
        /// </summary>
        public void ExtendTrackingWithSLAM(bool useSLAM)
        {
            bool restartTracking = (this.extendTrackingWithSLAM != useSLAM) &&
                                   this.trackingManager.GetTrackerInitialized();

            this.extendTrackingWithSLAM = useSLAM;

            if (restartTracking)
            {
                StartTracking();
            }
        }

        /// <summary>
        /// Set the URI of the tracking configuration file,
        /// which is used for the next tracking start.
        /// </summary>
        /// <remarks>
        /// Example: streaming-assets-dir:VisionLib/MyTracking.vl
        /// </remarks>
        public void SetConfigurationPath(string newURI)
        {
            this.configurationFileReference.uri = newURI;
        }

        /// <summary>
        /// Get the URI of the tracking configuration file,
        /// which is used to start tracking.
        /// </summary>
        public string GetConfigurationPath()
        {
            return this.configurationFileReference.uri;
        }

        /// <summary>
        /// Set the URI of the used license file.
        /// Will be applied on the next tracking start.
        /// </summary>
        /// <remarks>
        /// Example: streaming-assets-dir:VisionLib/license.xml
        /// </remarks>
        public void SetLicensePath(string newURI)
        {
            this.licenseFileReference.uri = newURI;
        }

        /// <summary>
        /// Get the URI of the used license file.
        /// </summary>
        public string GetLicensePath()
        {
            return this.licenseFileReference.uri;
        }

        /// <summary>
        /// Set the URI of the calibration file.
        /// Will be applied on the next tracking start.
        /// </summary>
        /// <remarks>
        /// Example: streaming-assets-dir:VisionLib/calibration.json
        /// </remarks>
        public void SetCalibrationPath(string newURI)
        {
            this.calibrationFileReference.uri = newURI;
        }

        /// <summary>
        /// Get the URI of the used calibration file.
        /// </summary>
        public string GetCalibrationPath()
        {
            return this.calibrationFileReference.uri;
        }
    }
}
