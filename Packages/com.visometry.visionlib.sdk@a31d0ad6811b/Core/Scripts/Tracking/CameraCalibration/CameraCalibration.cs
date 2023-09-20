using UnityEngine;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/Camera Calibration")]
    public class CameraCalibration : TrackingManagerReference
    {
        public TrackingConfiguration trackingConfiguration;

        /// <summary>
        ///  Location used to store the camera calibration.
        /// </summary>
        /// <remarks>
        ///  The <c>local-storage-dir</c> scheme can be, which points to a writable
        ///  location for each platform:
        ///  * Windows: Current users home directory
        ///  * MacOS: Current users document directory
        ///  * iOS / Android: The current applications document directory
        /// </remarks>
        public string destinationURI = "local-storage-dir:VisionLib/calibration.json";

        /// <summary>
        ///  Whether to offer a resolution selection.
        /// </summary>
        /// <remarks>
        ///  If this is <c>false</c>, then a default resolution will be used.
        ///  If this is <c>true</c>, then the user can select a specific
        ///   resolution, which will be used for the calibration.
        /// </remarks>
        public bool resolutionSelection = false;

        private enum State { Stopped, Running, Optimizing }
        private State state = State.Stopped;

        private CameraCalibrationResult calibration = null;
        private int frameCount = 0;

        /// <summary>
        ///  Used to scale the UI inside the OnGUI function.
        /// </summary>
        private GUIMatrixScaler guiScaler = new GUIMatrixScaler(640, 480);

        private TrackingConfiguration GetTrackingConfiguration()
        {
            if (this.trackingConfiguration == null)
            {
                this.trackingConfiguration = FindObjectOfType<TrackingConfiguration>();
            }

            return this.trackingConfiguration;
        }

        private void Awake()
        {
            if (GetTrackingConfiguration() == null)
            {
                LogHelper.LogError(
                    "Please add a `TrackingConfiguration` to your scene " +
                    "and reference the used calibration configuration file there.");
                return;
            }
            if (this.trackingConfiguration.autoStartTracking)
            {
                StartCalibration();
            }
        }

        private async Task StartChessboardDetectionAsync()
        {
            await CameraCalibrationCommands.RunAsync(this.worker);
            NotificationHelper.SendInfo("Calibration started");
        }

        /// <summary>
        /// Starts capturing of new images for the camera calibration.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void StartChessboardDetection()
        {
            TrackingManager.CatchCommandErrors(StartChessboardDetectionAsync(), this);
        }

        private async Task CancelOptimizationAsync()
        {
            await CameraCalibrationCommands.CancelAsync(this.worker);
            NotificationHelper.SendInfo("Calibration canceled");
            this.state = State.Running;
        }

        public void CancelOptimization()
        {
            TrackingManager.CatchCommandErrors(CancelOptimizationAsync(), this);
        }

        private async Task ResetCalibrationAsync()
        {
            this.calibration = null;
            await CameraCalibrationCommands.ResetAsync(this.worker);
            NotificationHelper.SendInfo("Calibration reset");
        }

        /// <summary>
        /// Resets the calibration and deletes all currently recorded images.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void ResetCalibration()
        {
            TrackingManager.CatchCommandErrors(ResetCalibrationAsync(), this);
        }

        private async Task PauseChessboardDetectionAsync()
        {
            await CameraCalibrationCommands.PauseAsync(this.worker);
            NotificationHelper.SendInfo("Calibration paused");
        }

        /// <summary>
        /// Pauses the capturing of new images for the calibration.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void PauseChessboardDetection()
        {
            TrackingManager.CatchCommandErrors(PauseChessboardDetectionAsync(), this);
        }

        /// <summary>
        /// Receives the Result of the camera calibration.
        /// </summary>
        private async Task<CameraCalibrationResult> GetOptimizationResultAsync()
        {
            CameraCalibrationResult calibration = null;
            while (calibration == null && this.state == State.Optimizing)
            {
                calibration = await CameraCalibrationCommands.GetResultsAsync(this.worker);
            }
            if (calibration == null)
            {
                NotificationHelper.SendWarning("Calibration process canceled");
                return null;
            }
            if (!calibration.calibrated)
            {
                NotificationHelper.SendError("Calibration failed");
                return null;
            }
            NotificationHelper.SendSuccess("Calibration was successful");
            LogHelper.LogDebug("Calibration Error: " + calibration.intrinsics.calibrationError);
            return calibration;
        }

        private async Task OptimizeCalibrationAsync()
        {
            await CameraCalibrationCommands.OptimizeAsync(this.worker);
            NotificationHelper.SendInfo("Calibration is optimizing");
            this.state = State.Optimizing;

            this.calibration = await GetOptimizationResultAsync();
            this.state = State.Running;
        }

        /// <summary>
        /// Performs the optimization with the currently recorder images.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void OptimizeCalibration()
        {
            TrackingManager.CatchCommandErrors(OptimizeCalibrationAsync(), this);
        }

        public async
            Task WriteCameraCalibrationAsync(string uri, CameraCalibrationResult calibration)
        {
            await CameraCalibrationCommands.WriteCameraCalibrationAsync(
                this.worker, uri, calibration);
            NotificationHelper.SendSuccess("Calibration written.");
        }

        /// <summary>
        /// Writes the successful camera calibration into the given file.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="uri">The URI to write the calibration file to.</param>
        /// <param name="calibration">The Calibration result to append to the file at the given
        /// uri.</param>
        public void WriteCameraCalibration(string uri, CameraCalibrationResult calibration)
        {
            TrackingManager.CatchCommandErrors(WriteCameraCalibrationAsync(uri, calibration), this);
        }

        public void StartCalibration()
        {
            if (GetTrackingConfiguration() == null)
            {
                return;
            }

            this.trackingConfiguration.StartTracking(useResolutionSelectionOverride
                                                     : this.resolutionSelection);
            this.state = State.Stopped;
        }

        private void OnGUI()
        {
            if (this.state == State.Stopped)
            {
                return;
            }

            this.guiScaler.Update();
            this.guiScaler.Set();

            // Calibration is running

            int buttonHeight = 20;
            int buttonWidth = 280;
            int buttonMarginBottom = 10;
            int xPos = 20;
            int yPos = 120;

            GUI.Box(
                new Rect(xPos, yPos, buttonWidth, buttonHeight + buttonMarginBottom),
                "Frames available for calibration: " + this.frameCount);
            yPos += buttonHeight + buttonMarginBottom + buttonMarginBottom;
            if (this.state == State.Optimizing)
            {
                if (GUI.Button(new Rect(xPos, yPos, buttonWidth, buttonHeight), "Stop"))
                {
                    CancelOptimization();
                }
            }
            else
            {
                if (GUI.Button(new Rect(xPos, yPos, buttonWidth, buttonHeight), "Run"))
                {
                    StartChessboardDetection();
                }
                yPos += buttonHeight + buttonMarginBottom;
                if (GUI.Button(new Rect(xPos, yPos, buttonWidth, buttonHeight), "Pause"))
                {
                    PauseChessboardDetection();
                }
                if (this.frameCount >= 200)
                {
                    yPos += buttonHeight + buttonMarginBottom;
                    if (GUI.Button(new Rect(xPos, yPos, buttonWidth, buttonHeight), "Calibrate"))
                    {
                        OptimizeCalibration();
                    }
                }
                if (this.calibration != null)
                {
                    yPos += 30;
                    this.destinationURI = GUI.TextField(
                        new Rect(xPos, yPos, buttonWidth, buttonHeight), this.destinationURI);

                    yPos += 30;
                    if (GUI.Button(new Rect(xPos, yPos, buttonWidth, buttonHeight), "Write"))
                    {
                        WriteCameraCalibration(this.destinationURI, this.calibration);
                    }
                }
                yPos += buttonHeight + buttonMarginBottom;
                if (GUI.Button(new Rect(xPos, yPos, buttonWidth, buttonHeight), "Restart"))
                {
                    ResetCalibration();
                }
            }

            this.guiScaler.Unset();
        }

        private void StoreTrackingStates(TrackingState trackingState)
        {
            if (trackingState.objects == null || trackingState.objects.Length == 0)
            {
                return;
            }
            TrackingState.TrackingObject obj = trackingState.objects[0];
            this.frameCount = obj._NumberOfPatternRecognitions;
        }

        private void HandleTrackerInitialized()
        {
            if (this.state == State.Stopped &&
                this.trackingManager.GetTrackerType() == "CameraCalibration")
            {
                this.state = State.Running;
            }
        }
        private void HandleTrackerStopped()
        {
            if (this.state == State.Running)
            {
                this.state = State.Stopped;
            }
        }

        private void OnEnable()
        {
            TrackingManager.OnTrackingStates += StoreTrackingStates;
            TrackingManager.OnTrackerInitialized += HandleTrackerInitialized;
            TrackingManager.OnTrackerStopped += HandleTrackerStopped;
        }

        private void OnDisable()
        {
            TrackingManager.OnTrackingStates -= StoreTrackingStates;
            TrackingManager.OnTrackerInitialized -= HandleTrackerInitialized;
            TrackingManager.OnTrackerStopped -= HandleTrackerStopped;
        }
    }
}
