using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  This behaviour is used by the TrackingConfiguration
    ///  to enable to select the input source that is used for tracking.
    ///  You can choose from a list of available devices or
    ///  use the input defined in your tracking configuration.
    /// </summary>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Input Source Selection")]
    public class InputSourceSelection : MonoBehaviour
    {
        private DeviceInfo.Camera selectedCamera;
        private bool showResolutionSelection = false;

        private enum SelectionState { CameraSelection, ResolutionSelection, None }
        private SelectionState selectionState = SelectionState.None;

        /// <summary>
        ///  Rectangle for the camera selection window. The actual values will get
        ///  determined automatically at runtime.
        /// </summary>
        private Rect windowRect;

        /// <summary>
        ///  Used to scale the UI inside the OnGUI function.
        /// </summary>
        private GUIMatrixScaler guiScaler = new GUIMatrixScaler(640, 480);

        /// <summary>
        ///  List of available cameras to select from.
        /// </summary>
        private DeviceInfo.Camera[] availableCameras;

        /// <summary>
        ///  Delegate for <see cref="OnInputSelected"/> event.
        /// </summary>
        public delegate void InputSelectedAction(InputSource inputSource);
        /// <summary>
        ///  Event which will be emitted after a device (and optionally the resolution)
        ///  has been selected from the GUI.
        ///  </summary>
        ///  <remarks>
        ///  The returned InputSource object holds the selected deviceID
        ///  and, if the resolution has been selected, the device format.
        ///  </remarks>
        public event InputSelectedAction OnInputSelected;

        /// <summary>
        ///  Event which will be emitted if the input selection
        ///  has been aborted.
        ///  </summary>
        public event VLSDK.VoidDelegate OnCanceled;

        public class InputSource
        {
            public string deviceID;
            public DeviceInfo.Camera.Format format;

            public InputSource(string deviceID = "", DeviceInfo.Camera.Format format = null)
            {
                this.deviceID = deviceID;
                this.format = format;
            }

            public string GetQueryString()
            {
                if (String.IsNullOrEmpty(this.deviceID))
                {
                    return "";
                }

                string query = "input.useDeviceID=" + this.deviceID;

                if (this.format != null)
                {
                    query = PathHelper.CombineQueryParameters(
                        "inputs[0].data.deviceID=" + this.deviceID,
                        PathHelper.CombineQueryParameters(
                            GetQueryStringFromCameraFormat(this.format)));
                }

                return query;
            }

            private List<string>
                GetQueryStringFromCameraFormat(DeviceInfo.Camera.Format cameraFormat)
            {
                // Calculate reasonable focal lengths, which can be used as initial
                // guess for a large range of cameras -> arithmetic mean of their
                // normalized values equals one
                double dw = cameraFormat.width;
                double dh = cameraFormat.height;
                double fnorm = 2.0 / (dw + dh);
                double fx = dh * fnorm;
                double fy = dw * fnorm;

                List<string> queryParameters = new List<string>();
                queryParameters.Add(
                    "inputs[0].data.calibration.width=" +
                    cameraFormat.width.ToString(CultureInfo.InvariantCulture));
                queryParameters.Add(
                    "inputs[0].data.calibration.height=" +
                    cameraFormat.height.ToString(CultureInfo.InvariantCulture));
                queryParameters.Add(
                    "inputs[0].data.calibration.fx=" +
                    fx.ToString("R", CultureInfo.InvariantCulture));
                queryParameters.Add(
                    "inputs[0].data.calibration.fy=" +
                    fy.ToString("R", CultureInfo.InvariantCulture));
                queryParameters.Add("inputs[0].data.calibration.cx=0.5");
                queryParameters.Add("inputs[0].data.calibration.cy=0.5");
                queryParameters.Add("inputs[0].type=camera");
                queryParameters.Add("inputs[0].data.SmartDownsamplingDisabled=true");

                return queryParameters;
            }
        }

        public void StartInputSelection(DeviceInfo deviceInfo, bool addResolutionSelection = false)
        {
            this.availableCameras = deviceInfo.availableCameras;
            this.selectionState = SelectionState.CameraSelection;
            this.showResolutionSelection = addResolutionSelection;
            this.windowRect = new Rect();
        }

        private void StartResolutionSelection()
        {
            this.selectionState = SelectionState.ResolutionSelection;
            this.windowRect = new Rect();
        }

        private void OnGUI()
        {
            switch (this.selectionState)
            {
                case SelectionState.CameraSelection:
                    DisplayWindow("Select your camera", DoInputSelectionWindow);
                    break;
                case SelectionState.ResolutionSelection:
                    DisplayWindow("Select your resolution", DoResolutionSelectionWindow);
                    break;
                case SelectionState.None:
                    break;
                default:
                    break;
            }
        }

        private void DisplayWindow(string label, GUI.WindowFunction windowFunction)
        {
            this.guiScaler.Update();
            this.guiScaler.Set();

            // (We call GUILayout.Window twice. In order to properly position
            // the window in the center of the screen)
            this.windowRect = GUILayout.Window(0, this.windowRect, windowFunction, label);
            this.windowRect.x =
                (this.guiScaler.GetScaledScreenRect().width - this.windowRect.width) / 2.0f;
            this.windowRect.y =
                (this.guiScaler.GetScaledScreenRect().height - this.windowRect.height) / 2.0f;
            this.windowRect = GUILayout.Window(0, this.windowRect, windowFunction, label);

            this.guiScaler.Unset();
        }

        /// <summary>
        /// Display a window with all available cameras
        /// </summary>
        private void DoInputSelectionWindow(int windowID)
        {
            // the state could have been changed inside the "DisplayWindow" function
            if (this.selectionState != SelectionState.CameraSelection)
            {
                return;
            }

            foreach (DeviceInfo.Camera camera in this.availableCameras)
            {
                if (GUILayout.Button(camera.cameraName))
                {
                    this.selectedCamera = camera;
                    ApplyInputSelection(new InputSource(this.selectedCamera.deviceID));
                    return;
                }
            }

            if (GUILayout.Button("Tracking Config Input"))
            {
                this.selectedCamera = null;
                ApplyInputSelection(new InputSource(""));
                return;
            }

            if (GUILayout.Button("Cancel"))
            {
                Cancel();
            }
        }

        private void ApplyInputSelection(InputSource inputSource)
        {
            if (this.selectedCamera != null)
            {
                LogHelper.LogInfo("Selected Camera: " + this.selectedCamera + "\n");
            }

            if (this.showResolutionSelection && !String.IsNullOrEmpty(inputSource.deviceID))
            {
                StartResolutionSelection();
            }
            else
            {
                OnInputSelected?.Invoke(inputSource);
                this.selectionState = SelectionState.None;
            }
        }

        /// <summary>
        ///  Creates selection window for all available resolutions
        ///  of the selected camera.
        ///  Only working on Windows.
        /// </summary>
        private void DoResolutionSelectionWindow(int windowID)
        {
            // the state could have been changed inside the "DisplayWindow" function
            if (this.selectionState != SelectionState.ResolutionSelection)
            {
                return;
            }

            if (this.selectedCamera == null)
            {
                Cancel();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            // Add a button for each possible resolution
            int buttonRow = 1;
            foreach (DeviceInfo.Camera.Format format in this.selectedCamera.availableFormats)
            {
                if (GUILayout.Button(format.ToString()))
                {
                    ApplyResolutionSelection(new InputSource(this.selectedCamera.deviceID, format));
                    return;
                }

                // Only show 10 buttons per column
                ++buttonRow;
                if (buttonRow >= 10)
                {
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    buttonRow = 0;
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // Default button
            if (GUILayout.Button("Default"))
            {
                ApplyResolutionSelection(new InputSource(this.selectedCamera.deviceID, null));
                return;
            }

            if (GUILayout.Button("Cancel"))
            {
                Cancel();
            }
        }

        private void ApplyResolutionSelection(InputSource inputSource)
        {
            OnInputSelected?.Invoke(inputSource);
            this.selectionState = SelectionState.None;
        }

        public void Cancel()
        {
            OnCanceled?.Invoke();
            this.selectionState = SelectionState.None;
        }
    }
}
