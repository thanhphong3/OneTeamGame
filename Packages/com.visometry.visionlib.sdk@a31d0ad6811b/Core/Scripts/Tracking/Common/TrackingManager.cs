using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/Tracking Manager")]
    public class TrackingManager : MonoBehaviour
    {
        /// <summary>
        /// Directory to which file uris will be loaded relative to.
        /// This includes the tracking configuration, the license
        /// and the calibration file.
        /// </summary>
        /// <remarks>
        /// The base directories value is "streaming-assets-dir:VisionLib".
        /// Loading files from the <c>/StreamingAssets/</c> directory
        /// has the advantage, that they can be found on all platforms.
        /// </remarks>
        private static readonly string baseDir = "streaming-assets-dir:VisionLib";

        /// <summary>
        /// Path of the license file.
        /// </summary>
        [Tooltip("Path of the license file, e.g. 'streaming-assets-dir:VisionLib/license.xml.")]
        public LicenseFile licenseFile;

        public string calibrationDataBaseURI;

        protected object workerLock = new object();

        private bool trackingRunning = false;

        public bool GetTrackingRunning()
        {
            return this.trackingRunning;
        }

        /// <summary>
        /// This variable stores the state of the trackerInitialized variable before it is Disabled.
        /// </summary>
        /// <remarks>
        /// Some MonoBehaviours request the trackerInitialized state in their OnEnable function. If
        /// OnEnable is called for several GameObjects, the execution order can not be influenced.
        /// This is especially the case when recompiling during play mode. For this reason, we store
        /// the trackerInitialized state in this variable and set trackerInitialized to false in
        /// OnDisable. In OnEnable, we reset the value of trackerInitialized back to the original
        /// value. This way, any MonoBehaviour re-enabled before the TrackingManager will assume the
        /// TrackingManager as uninitialized.
        /// At the moment, if the scripts are recompiled, TrackingManager will be uninitialized
        /// afterwards.
        /// </remarks>
        private bool previousTrackerInitialized = false;
        private bool trackerInitialized = false;
        public bool GetTrackerInitialized()
        {
            return this.trackerInitialized;
        }

        private string trackerType = "";
        /// <summary>
        /// Returns the type of the loaded tracking pipeline.
        /// Works for tracking configurations loaded from a vl-file or vl-string.
        /// </summary>
        /// <returns>loaded tracker type</returns>
        public string GetTrackerType()
        {
            if (!GetTrackerInitialized())
            {
                return "";
            }
            return this.trackerType;
        }

        private string deviceType = "";
        /// <summary>
        /// Returns the type of the loaded device pipeline.
        /// Works for tracking configurations loaded from a vl-file or vl-string.
        /// </summary>
        /// <returns>loaded device type</returns>
        public string GetDeviceType()
        {
            if (!GetTrackerInitialized())
            {
                return "";
            }
            return this.deviceType;
        }

        /// <summary>
        ///  Event which will be emitted once after calling the StartTracking
        ///  function.
        /// </summary>
        public static event VLSDK.VoidDelegate OnTrackerInitializing;

        /// <summary>
        ///  Event which will be emitted after the tracking configuration was
        ///  loaded.
        /// </summary>
        public static event VLSDK.VoidDelegate OnTrackerInitialized;

        /// <summary>
        ///  Event which will be emitted after the tracking was stopped or
        ///  the initialization of the tracker has failed.
        /// </summary>
        public static event VLSDK.VoidDelegate OnTrackerStopped;

        /// <summary>
        ///  Event which will be emitted once after the tracking was stopped or
        ///  paused and is now running again.
        /// </summary>
        public static event VLSDK.VoidDelegate OnTrackerRunning;

        /// <summary>
        ///  Event which will be emitted after the tracking was paused.
        /// </summary>
        public static event VLSDK.VoidDelegate OnTrackerPaused;

        /// <summary>
        ///  Event which will be emitted after a soft reset was executed.
        /// </summary>
        [System.Obsolete(
            "The `OnTrackerResetSoft` event is obsolete. If you want to reset the tracking and perform some code afterwards, please use the `ResetSoft` function returning a task.")]
        public static event VLSDK.VoidDelegate OnTrackerResetSoft;

        /// <summary>
        ///  Event which will be emitted after a hard reset was executed.
        /// </summary>
        [System.Obsolete(
            "The `OnTrackerResetHard` event is obsolete. If you want to reset the tracking and perform some code afterwards, please use the `ResetHard` function returning a task.")]
        public static event VLSDK.VoidDelegate OnTrackerResetHard;

        /// <summary>
        ///  Delegate for <see cref="OnTrackingStates"/> events.
        /// </summary>
        /// <param name="state">
        ///  <see cref="TrackingState"/> with information about the currently
        ///  tracked objects.
        /// </param>
        public delegate void TrackingStatesAction(TrackingState state);
        /// <summary>
        ///  Event with the current tracking state of all tracked objects. This
        ///  Event will be emitted for each tracking frame.
        /// </summary>
        public static event TrackingStatesAction OnTrackingStates;

        /// <summary>
        ///  Delegate for <see cref="OnPerformanceInfo"/> events.
        /// </summary>
        /// <param name="state">
        ///  <see cref="PerformanceInfo"/> with information about the performance.
        /// </param>
        public delegate void PerformanceInfoAction(PerformanceInfo state);
        /// <summary>
        ///  Event with the current tracking performance. This Event will be
        ///  emitted for each tracking frame.
        /// </summary>
        public static event PerformanceInfoAction OnPerformanceInfo;

        /// <summary>
        /// Delegate for <see cref="OnImage"/> events.
        /// </summary>
        /// <param name="image">
        ///  <see cref="Image"/>.
        /// </param>
        public delegate void ImageAction(Image image);
        /// <summary>
        ///  Event with the current tracking image. This Event will be
        ///  emitted for each tracking frame.
        /// </summary>
        public static event ImageAction OnImage;
        /// <summary>
        ///  Event with the current debug image. This Event will be
        ///  emitted for each tracking frame, if debugLevel is at least 1
        /// </summary>
        public static event ImageAction OnDebugImage;

        /// <summary>
        /// Delegate for <see cref="OnExtrinsicData"/> events.
        /// </summary>
        /// <param name="extrinsicData">
        /// <see cref="ExtrinsicData"/>.
        /// </param>
        public delegate void ExtrinsicDataAction(ExtrinsicData extrinsicData);
        /// <summary>
        ///  Event with the current extrinsic data. This Event will be
        ///  emitted for each tracking frame.
        /// </summary>
        public static event ExtrinsicDataAction OnExtrinsicData;
        /// <summary>
        ///  Event with the current extrinsic data slam. This Event will be
        ///  emitted for each tracking frame.
        /// </summary>
        public static event ExtrinsicDataAction OnCameraTransform;

        internal static AnchorObservableMap anchorObservableMap = new AnchorObservableMap();

        private NotificationAdapter notificationAdapter = new NotificationAdapter();

        public static EventWrapper<SimilarityTransform> AnchorTransform(string anchorName)
        {
            return anchorObservableMap.GetOrCreate(anchorName);
        }

        private Dictionary<string, IDisposable> anchorTransformListeners =
            new Dictionary<string, IDisposable>();

        virtual protected void UpdateAnchorTransformListeners()
        {
            if (!this.trackerInitialized)
            {
                return;
            }
            anchorObservableMap.SynchronizeHandler(this.anchorTransformListeners, this.Worker);
        }

        private void UnregisterAllAnchorTransformListeners()
        {
            foreach (var listenerKV in this.anchorTransformListeners)
            {
                listenerKV.Value.Dispose();
            }
            this.anchorTransformListeners.Clear();
        }

        /// <summary>
        /// Delegate for <see cref="OnIntrinsicData"/> events.
        /// </summary>
        /// <param name="intrinsicData">
        /// <see cref="IntrinsicData"/>.
        /// </param>
        public delegate void IntrinsicDataAction(IntrinsicData intrinsicData);
        /// <summary>
        ///  Event with the current intrinsic data. This Event will be
        ///  emitted for each tracking frame.
        /// </summary>
        public static event IntrinsicDataAction OnIntrinsicData;

        /// <summary>
        /// Delegate for <see cref="OnCalibratedImage"/> events.
        /// </summary>
        /// <param name="calibratedImage">
        /// <see cref="CalibratedImage"/>.
        /// </param>
        public delegate void CalibratedImageAction(CalibratedImage calibratedImage);
        /// <summary>
        ///  Event with the current calibrated depth image. This Event will be
        ///  emitted for each tracking frame.
        /// </summary>
        public static event CalibratedImageAction OnCalibratedDepthImage;

        /// <summary>
        ///  Delegate for <see cref="OnIssueTriggered"/> events.
        /// </summary>
        /// <param name="issue">
        /// <see cref="Issue"/>
        /// </param>
        public delegate void IssueTriggeredAction(Issue issue);
        /// <summary>
        ///  Event which will be emitted if an Issue was triggered
        /// </summary>
        public static event IssueTriggeredAction OnIssueTriggered;

        private static void TriggerIssue(Issue issue)
        {
            OnIssueTriggered?.Invoke(issue);
        }

        /// <summary>
        ///  Target number of frames per second for the tracking thread.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   The tracking will run as fast as possible, if the value is zero or
        ///   less.
        ///  </para>
        ///  <para>
        ///   Higher values will result in a smoother tracking experience, but the
        ///   battery will be drained faster.
        ///  </para>
        /// </remarks>
        [Tooltip("Target number of frames per second for tracking.")]
        public int targetFPS = 30;
        private int lastTargetFPS = -1;

        /// <summary>
        ///  Whether to wait for tracking events.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   If <c>true</c>, the Update member function will wait until there is
        ///   at least one tracking event. This will limit the speed of the Unity
        ///   update cycler to the speed of the tracking, but the tracking will feel
        ///   more smooth, because the camera image will be shown with less delay.
        ///  </para>
        ///  <para>
        ///   If <c>false</c>, the speed of the tracking and the Unity update cycle
        ///   are largely separate. Due to the out of sync update rates, the camera
        ///   might be shown with a slight delay.
        ///  </para>
        /// </remarks>
        [Tooltip("Update only if there is at least one tracking event.")]
        public bool waitForEvents = false;

        /// <summary>
        ///  VisionLib log level.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   Available log levels:
        ///   * 0: Mute
        ///   * 1: Fatal
        ///   * 2: Warning
        ///   * 3: Notice
        ///   * 4: Info
        ///   * 5: Debug
        ///  </para>
        ///  <para>
        ///   Log level N will disable all log messages with a level > N.
        ///  </para>
        /// </remarks>
        [Tooltip("Log Types")]
        public VLSDK.LogLevel logLevel = VLSDK.LogLevel.Warning;
        private VLSDK.LogLevel lastLogLevel;

        private API.Logger logger = null;
        private GCHandle gcHandle;

        /// <summary>
        ///  Event which will be emitted once after the worker
        ///  has been created.
        /// </summary>
        public static event VLSDK.VoidDelegate OnWorkerCreated;

        protected Worker worker = null;
        protected Worker Worker
        {
            get
            {
                if (this.worker == null)
                {
                    throw new WorkerNotFoundException();
                }
                return this.worker;
            }
        }

        public class WorkerNotFoundException : Exception
        {
            /// <summary>
            /// Exception that is thrown when the worker is tried to be accessed while it is null.
            /// This can happen when the worker is already destroyed or not created yet.
            /// </summary>
            public WorkerNotFoundException() :
                base(
                    "The Worker is accessed before it has been created or after it has been destroyed.")
            {
            }
        }

        /// <summary>
        ///  Returns the owned Worker object.
        /// </summary>
        /// <returns>
        ///  Worker object or null, if the Worker wasn't initialized yet.
        /// </returns>
        public Worker GetWorker()
        {
            return this.Worker;
        }

        private string MergeWithBaseDirIfRelative(string uri)
        {
            if (!PathHelper.IsAbsolutePath(uri))
            {
                // Avoid breaking changes, especially for users that had specified
                // the license relative to StreamingAssets, e.g. "VisionLib/myLicense.xml"
                string pathWithoutVLPrefix = uri;
                if (uri.StartsWith("VisionLib/"))
                {
                    pathWithoutVLPrefix = uri.Substring("VisionLib/".Length);
                }

                string mergedUri = PathHelper.CombinePaths(baseDir, pathWithoutVLPrefix);
                LogHelper.LogWarning(
                    "Loading files relative to a base directory is deprecated." +
                        "\nPlease use an absolute path or an URI scheme instead, e.g. '" +
                        mergedUri + "'.",
                    this);
                return mergedUri;
            }
            return uri;
        }

        /// <summary>
        /// Adds the camera calibration DataBase using the URI. It will not be loaded at this point
        /// but only the possibility to add it will be checked. The loading of the actual database
        /// happens when starting the tracking pipe!
        ///
        /// </summary>
        /// <returns><c>true</c>, if camera calibration DB was added, <c>false</c>
        /// otherwise.</returns> <param name="uri">URI pointing to the camera calibration to be
        /// merged.</param>
        public bool AddCameraCalibrationDB(string uri)
        {
            return this.Worker.AddCameraCalibrationDB(MergeWithBaseDirIfRelative(uri));
        }

        /// <summary>
        /// Sets the path of the license file.
        /// </summary>
        /// <returns>
        ///  <c>true</c>, if a valid license file could be found;
        ///  <c>false</c> otherwise.</returns>
        private void ApplyLicenseFilePath()
        {
            if (this.licenseFile.path.Length > 0 && this.licenseFile.content.Length > 0)
            {
                LogHelper.LogWarning(
                    "Both license file and license data are specified. Using file: " +
                    this.licenseFile.path);
            }

            if (Regex.IsMatch(licenseFile.path, "\\*\\*.*\\*\\*"))
            {
                this.Worker.SetLicenseFilePath(this.licenseFile.path);
                return;
            }

            if (this.licenseFile.path.Length > 0)
            {
                this.Worker.SetLicenseFilePath(MergeWithBaseDirIfRelative(licenseFile.path));
            }
            else if (this.licenseFile.content.Length > 0)
            {
                this.Worker.SetLicenseFileData(this.licenseFile.content);
            }
            else
            {
                this.Worker.SetLicenseFilePath("");
            }
        }

        /// <summary>
        ///  Start the tracking using a vl-file.
        /// </summary>
        /// <remarks>
        ///  The type of the tracker will be derived from the vl-file.
        /// </remarks>
        public virtual void StartTracking(string filename)
        {
            StopTracking();
            ApplyLicenseFilePath();

            if (this.calibrationDataBaseURI != "")
            {
                AddCameraCalibrationDB(this.calibrationDataBaseURI);
            }

            LogHelper.LogDebug("Tracker initializing");
            OnTrackerInitializing?.Invoke();

            this.Worker.Start();

            string trackingFile = MergeWithBaseDirIfRelative(filename);
            StartTracker(WorkerCommands.CreateTrackerAsync(this.worker, trackingFile));
        }

        /// <summary>
        ///  Start the tracking using a tracking configuration as string.
        /// </summary>
        /// <param name="trackingConfig">Tracking configuration as string</param>
        /// <param name="projectDir">Directory</param>
        /// <param name="overrideParameter"></param>
        public void StartTrackingFromString(
            string trackingConfig,
            string projectDir,
            string overrideParameter = null)
        {
            StopTracking();
            ApplyLicenseFilePath();

            if (this.calibrationDataBaseURI != "")
            {
                AddCameraCalibrationDB(this.calibrationDataBaseURI);
            }

            OnTrackerInitializing?.Invoke();

            if (String.IsNullOrEmpty(projectDir))
            {
                LogHelper.LogWarning(
                    "The project directory has not been set. File references will be searched in " +
                    baseDir);
                projectDir = baseDir;
            }

            this.Worker.Start();

            string basePathFileName = PathHelper.CombinePaths(projectDir, "FakeFileName.vl");
            if (!String.IsNullOrEmpty(overrideParameter) && !overrideParameter.StartsWith("?"))
            {
                basePathFileName += "?";
            }
            basePathFileName += overrideParameter;

            StartTracker(
                WorkerCommands.CreateTrackerAsync(this.worker, trackingConfig, basePathFileName));
        }

        private async Task StartTrackerAsync(Task<TrackerInfo> createTrackerTask)
        {
            try
            {
                var trackerInfo = await createTrackerTask;
                this.trackerType = trackerInfo.trackerType;
                this.deviceType = trackerInfo.deviceType;
                this.trackerInitialized = true;

                if (trackerInfo.warnings != null)
                {
                    foreach (var initWarning in trackerInfo.warnings)
                    {
                        initWarning.caller = this;
                        initWarning.level = Issue.IssueType.Warning;
                        TriggerIssue(initWarning);
                    }
                }

                NotificationHelper.SendInfo("Tracker initialized");

                LogHelper.LogDebug("Tracker Type: " + this.trackerType);
                LogHelper.LogDebug("Device Type: " + this.deviceType);

                OnTrackerInitialized?.Invoke();

                SetFPS(this.targetFPS);
                InitializeImageStreams();
                RegisterTrackerListeners();
                RunTracker();
            }
            catch (WorkerCommands.CommandError e)
            {
                this.trackerInitialized = false;
                var initIssue = e.GetIssue();
                initIssue.caller = this;
                TriggerIssue(initIssue);
                StopTracking();
                OnTrackerStopped?.Invoke();
                return;
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <summary>
        /// Starts the given tracker, after it was created.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="createTrackerTask">Task for creating the tracker.</param>
        private void StartTracker(Task<TrackerInfo> createTrackerTask)
        {
            CatchCommandErrors(StartTrackerAsync(createTrackerTask), this);
        }

        private async Task RunTrackerAsync()
        {
            await WorkerCommands.RunTrackingAsync(this.worker);
            this.trackingRunning = true;
            LogHelper.LogDebug("Tracker running");
            OnTrackerRunning?.Invoke();
        }

        private void RunTracker()
        {
            CatchCommandErrors(RunTrackerAsync(), this);
        }

        /// <summary>
        ///  Stop the tracking (releases all tracking resources).
        /// </summary>
        public virtual void StopTracking()
        {
            var wasInitialized = this.trackerInitialized;
            this.Worker.Stop();
            this.trackingRunning = false;
            this.trackerInitialized = false;
            DeinitializeImageStreams();
            UnregisterTrackerListeners();
            if (wasInitialized)
            {
                OnTrackerStopped?.Invoke();
                NotificationHelper.SendInfo("Tracker stopped");
            }
        }

        /// <summary>
        ///  Pause the tracking.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void PauseTracking()
        {
            CatchCommandErrors(PauseTrackingAsync(), this);
        }

        /// <summary>
        ///  Pause the tracking.
        /// </summary>
        public async Task PauseTrackingAsync()
        {
            this.trackingRunning = false;
            await PauseTrackingInternalAsync();
        }

        /// <summary>
        ///  Pause the tracking internal.
        ///  Does not modify the `trackingRunning` variable.
        /// </summary>
        private async Task PauseTrackingInternalAsync()
        {
            await WorkerCommands.PauseTrackingAsync(this.worker);
            OnTrackerPaused?.Invoke();
            NotificationHelper.SendInfo("Tracker paused");
        }

        /// <summary>
        /// Pause the tracking.
        /// Does not modify the `trackingRunning` variable.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        private void PauseTrackingInternal()
        {
            CatchCommandErrors(PauseTrackingInternalAsync(), this);
        }

        /// <summary>
        ///  Resume the tracking.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void ResumeTracking()
        {
            CatchCommandErrors(ResumeTrackingAsync(), this);
        }

        /// <summary>
        ///  Resume the tracking.
        /// </summary>
        public async Task ResumeTrackingAsync()
        {
            await RunTrackerAsync();
        }

        /// <summary>
        ///  Sets target number of frames per second for the tracking thread.
        /// </summary>
        public async Task SetFPSAsync(int newFPS)
        {
            if (trackerInitialized)
            {
                this.targetFPS = newFPS;
                this.lastTargetFPS = newFPS;
                await WorkerCommands.SetTargetFPSAsync(this.worker, newFPS);
                LogHelper.LogDebug("Set FPS to " + newFPS);
            }
        }

        /// <summary>
        /// Sets the frame rate of the tracking algorithm.
        /// This might limit the performance consumed by VisionLib.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="newFPS">Number of frames per second</param>
        public void SetFPS(int newFPS)
        {
            CatchCommandErrors(SetFPSAsync(newFPS), this);
        }

        [System.Obsolete(
            "Do not use this function. It has only been introduced to support previous behaviour and will be removed in the future.")]
        public static void InvokeOnTrackerResetSoft()
        {
#pragma warning disable CS0618 // OnTrackerResetSoft is obsolete
            OnTrackerResetSoft?.Invoke();
#pragma warning restore CS0618 // OnTrackerResetSoft is obsolete
        }

        private async Task ResetTrackingSoftAsync()
        {
            await ModelTrackerCommands.ResetSoftAsync(this.worker);
#pragma warning disable CS0618 // OnTrackerResetSoft is obsolete
            InvokeOnTrackerResetSoft();
#pragma warning restore CS0618 // OnTrackerResetSoft is obsolete
            NotificationHelper.SendInfo("Tracker reset init pose");
        }

        /// <summary>
        ///  Reset the tracking.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        [System.Obsolete(
            "The `void ResetTrackingHard()` function of TrackingManager is obsolete. Use the reset functions of the active Tracker instead (e.g. ModelTracker).")]
        public void ResetTrackingSoft()
        {
            CatchCommandErrors(ResetTrackingSoftAsync(), this);
        }

        [System.Obsolete(
            "Do not use this function. It has only been introduced to support previous behaviour and will be removed in the future.")]
        public static void InvokeOnTrackerResetHard()
        {
#pragma warning disable CS0618 // OnTrackerResetHard is obsolete
            OnTrackerResetHard?.Invoke();
#pragma warning restore CS0618 // OnTrackerResetHard is obsolete
        }

        private async Task ResetTrackingHardAsync()
        {
            await ModelTrackerCommands.ResetHardAsync(this.worker);
#pragma warning disable CS0618 // OnTrackerResetHard is obsolete
            InvokeOnTrackerResetHard();
#pragma warning restore CS0618 // OnTrackerResetHard is obsolete
            NotificationHelper.SendInfo("Tracker reset");
        }

        /// <summary>
        ///  Reset the tracking and all key frames.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        [System.Obsolete(
            "The `void ResetTrackingHard()` function of TrackingManager is obsolete. Use the reset functions of the active Tracker instead (e.g. ModelTracker).")]
        public void ResetTrackingHard()
        {
            CatchCommandErrors(ResetTrackingHardAsync(), this);
        }

        /// <summary>
        ///  Set <see cref="waitForEvents"/> to the given value.
        /// </summary>
        /// <remarks>
        ///  See <see cref="waitForEvents"/> for further information.
        /// </remarks>
        public void SetWaitForEvents(bool wait)
        {
            this.waitForEvents = wait;
        }

        /// <summary>
        /// Returns the device info, when the worker object has been initialized.
        /// You can call this function in order to get useful system information before starting the
        /// tracking pipe You might use this structure for retrieving the available cameras in the
        /// system.
        /// </summary>
        /// <returns>The device info object or null.</returns>
        public DeviceInfo GetDeviceInfo()
        {
            return this.Worker.GetDeviceInfo();
        }

        /// <summary>
        /// Returns the type of the loaded tracking pipeline.
        /// Works for tracking configurations loaded from a vl-file or vl-string.
        /// </summary>
        /// <param name="trackerType">loaded tracker type</param>
        /// <returns>returns true on success; false otherwise.</returns>
        [System.Obsolete(
            "The `bool GetTrackerType(out string trackerType)` function is obsolete. Use `string GetTrackerType()` instead.")]
        public bool GetTrackerType(out string trackerType)
        {
            trackerType = GetTrackerType();
            return trackerType != "";
        }

        private static TrackingManager GetInstance(IntPtr clientData)
        {
            return (TrackingManager) GCHandle.FromIntPtr(clientData).Target;
        }

        [MonoPInvokeCallback(typeof(Worker.ImageWrapperCallback))]
        private static void DispatchImageCallback(IntPtr handle, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).ImageHandler(handle);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }
        private static Worker.ImageWrapperCallback dispatchImageCallbackDelegate =
            new Worker.ImageWrapperCallback(DispatchImageCallback);

        [MonoPInvokeCallback(typeof(Worker.ImageWrapperCallback))]
        private static void DispatchDebugImageCallback(IntPtr handle, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).DebugImageHandler(handle);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }
        private static Worker.ImageWrapperCallback dispatchDebugImageCallbackDelegate =
            new Worker.ImageWrapperCallback(DispatchDebugImageCallback);

        [MonoPInvokeCallback(typeof(Worker.ExtrinsicDataWrapperCallback))]
        private static void DispatchExtrinsicDataCallback(IntPtr handle, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).ExtrinsicDataHandler(handle);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }
        private static Worker.ExtrinsicDataWrapperCallback dispatchExtrinsicDataCallbackDelegate =
            new Worker.ExtrinsicDataWrapperCallback(DispatchExtrinsicDataCallback);

        [MonoPInvokeCallback(typeof(Worker.ExtrinsicDataWrapperCallback))]
        private static void DispatchCameraTransformCallback(IntPtr handle, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).CameraTransformHandler(handle);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }
        private static Worker.ExtrinsicDataWrapperCallback dispatchCameraTransformCallbackDelegate =
            new Worker.ExtrinsicDataWrapperCallback(DispatchCameraTransformCallback);

        [MonoPInvokeCallback(typeof(Worker.IntrinsicDataWrapperCallback))]
        private static void DispatchIntrinsicDataCallback(IntPtr handle, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).IntrinsicDataHandler(handle);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }
        private static Worker.IntrinsicDataWrapperCallback dispatchIntrinsicDataCallbackDelegate =
            new Worker.IntrinsicDataWrapperCallback(DispatchIntrinsicDataCallback);

        [MonoPInvokeCallback(typeof(Worker.CalibratedImageWrapperCallback))]
        private static void DispatchCalibratedDepthImageCallback(IntPtr handle, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).CalibratedDepthImageHandler(handle);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        private static Worker
            .CalibratedImageWrapperCallback dispatchCalibratedDepthImageCallbackDelegate =
            new Worker.CalibratedImageWrapperCallback(DispatchCalibratedDepthImageCallback);

        [MonoPInvokeCallback(typeof(Worker.StringCallback))]
        private static void
            DispatchTrackingStateCallback(string trackingStateJson, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).TrackingStateHandler(trackingStateJson);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }
        private static Worker.StringCallback dispatchTrackingStateCallbackDelegate =
            new Worker.StringCallback(DispatchTrackingStateCallback);

        [MonoPInvokeCallback(typeof(Worker.StringCallback))]
        private static void
            DispatchPerformanceInfoCallback(string performanceInfoJson, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).PerformanceInfoHandler(performanceInfoJson);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }
        private static Worker.StringCallback dispatchPerformanceInfoCallbackDelegate =
            new Worker.StringCallback(DispatchPerformanceInfoCallback);

        private void ImageHandler(IntPtr handle)
        {
            using (Image image = new Image(handle, false))
            {
                OnImage?.Invoke(image);
            }
        }

        private void DebugImageHandler(IntPtr handle)
        {
            using (Image debugImage = new Image(handle, false))
            {
                OnDebugImage?.Invoke(debugImage);
            }
        }

        private void ExtrinsicDataHandler(IntPtr handle)
        {
            using (ExtrinsicData extrinsicData = new ExtrinsicData(handle, false))
            {
                OnExtrinsicData?.Invoke(extrinsicData);
            }
        }

        private void CameraTransformHandler(IntPtr handle)
        {
            using (ExtrinsicData extrinsicData = new ExtrinsicData(handle, false))
            {
                OnCameraTransform?.Invoke(extrinsicData);
            }
        }

        private void IntrinsicDataHandler(IntPtr handle)
        {
            using (IntrinsicData intrinsicData = new IntrinsicData(handle, false))
            {
                OnIntrinsicData?.Invoke(intrinsicData);
            }
        }

        private void CalibratedDepthImageHandler(IntPtr handle)
        {
            using (CalibratedImage calibratedImage = new CalibratedImage(handle, false))
            {
                OnCalibratedDepthImage?.Invoke(calibratedImage);
            }
        }

        private void TrackingStateHandler(string trackingStateJson)
        {
            TrackingState state = JsonHelper.FromJson<TrackingState>(trackingStateJson);
            if (state != null)
            {
                OnTrackingStates?.Invoke(state);
            }
        }

        private void PerformanceInfoHandler(string performanceInfoJson)
        {
            PerformanceInfo performanceInfo =
                JsonHelper.FromJson<PerformanceInfo>(performanceInfoJson);
            OnPerformanceInfo?.Invoke(performanceInfo);
        }

        private void ProcessCallbacks()
        {
            this.Worker.ProcessCallbacks();
            if (this.waitForEvents)
            {
                this.Worker.WaitEvents(1000);
            }
            else
            {
                this.Worker.PollEvents();
            }
        }

        private static readonly int supportedMajorVersion = 2;
        private static readonly int minimalMinorVersion = 2;

        private bool IsMajorVersionSupported()
        {
            return VLSDK.GetMajorVersion() == supportedMajorVersion;
        }

        private bool IsMinorVersionSupported()
        {
            return IsMajorVersionSupported() && VLSDK.GetMinorVersion() >= minimalMinorVersion;
        }

        protected virtual void CreateWorker()
        {
            this.worker = new Worker(false);
        }

        protected void CreateLogger()
        {
            this.logger = new API.Logger();
            // Unity 2017 with Mono .NET 4.6 as scripting runtime version can't
            // properly handle callbacks from external threads. Until this is
            // fixed, we need to buffer the log messages and fetch them from the
            // main thread inside the update function.
            this.logger.EnableLogBuffer();
            this.logger.SetLogLevel(this.logLevel);
            this.lastLogLevel = this.logLevel;
        }

        protected virtual void RegisterListeners()
        {
            if (!this.Worker.AddImageListener(
                    dispatchImageCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to add image listener");
            }
            if (!this.Worker.AddExtrinsicDataListener(
                    dispatchExtrinsicDataCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to add extrinsic data listener");
            }
            if (!this.Worker.AddIntrinsicDataListener(
                    dispatchIntrinsicDataCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to add intrinsic data listener");
            }
            if (!this.Worker.AddCalibratedImageListener(
                    dispatchCalibratedDepthImageCallbackDelegate,
                    GCHandle.ToIntPtr(this.gcHandle),
                    VLSDK.ImageFormat.Depth))
            {
                LogHelper.LogWarning("Failed to add depth image listener");
            }
            if (!this.Worker.AddTrackingStateListener(
                    dispatchTrackingStateCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to add tracking state listener");
            }
            if (!this.Worker.AddPerformanceInfoListener(
                    dispatchPerformanceInfoCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to add performance info listener");
            }
        }

        protected virtual void UnregisterListeners()
        {
            // Explicitly remove the listeners, so we know if everything went well
            if (!this.Worker.RemovePerformanceInfoListener(
                    dispatchPerformanceInfoCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to remove performance info listener");
            }
            if (!this.Worker.RemoveTrackingStateListener(
                    dispatchTrackingStateCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to remove tracking state listener");
            }
            if (!this.Worker.RemoveCalibratedImageListener(
                    dispatchCalibratedDepthImageCallbackDelegate,
                    GCHandle.ToIntPtr(this.gcHandle),
                    VLSDK.ImageFormat.Depth))
            {
                LogHelper.LogWarning("Failed to remove depth frame listener");
            }
            if (!this.Worker.RemoveIntrinsicDataListener(
                    dispatchIntrinsicDataCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to remove intrinsic data listener");
            }
            if (!this.Worker.RemoveExtrinsicDataListener(
                    dispatchExtrinsicDataCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to remove extrinsic data listener");
            }
            if (!this.Worker.RemoveImageListener(
                    dispatchImageCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to remove image listener");
            }
        }

        private bool worldFromCameraTransformListenerRegistered = false;
        protected virtual void RegisterTrackerListeners()
        {
            this.worldFromCameraTransformListenerRegistered =
                this.Worker.AddWorldFromCameraTransformListener(
                    dispatchCameraTransformCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle));
        }

        protected virtual void UnregisterTrackerListeners()
        {
            UnregisterAllAnchorTransformListeners();
            if (this.worldFromCameraTransformListenerRegistered &&
                !this.Worker.RemoveWorldFromCameraTransformListener(
                    dispatchCameraTransformCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                LogHelper.LogWarning("Failed to remove extrinsic data slam listener");
            }
            this.worldFromCameraTransformListenerRegistered = false;
        }

        private void OnEnable()
        {
            Initialize();

            if (this.trackerInitialized)
            {
                ResumeTracking();
            }

            this.notificationAdapter.ActivateNotifications();
        }

        private void OnDisable()
        {
            this.previousTrackerInitialized = this.trackerInitialized;

            // Check for this.enabled will check whether the GameObject is about to be destroyed
            if (this.trackerInitialized && !this.enabled)
            {
                PauseTracking();
            }

            this.notificationAdapter.DeactivateNotifications();

            this.trackerInitialized = false;
        }

        private void Initialize()
        {
            if (this.worker != null)
            {
                this.trackerInitialized = this.previousTrackerInitialized;
                return;
            }

            DeinitializeImageStreams();
            UnregisterAllAnchorTransformListeners();
            this.worldFromCameraTransformListenerRegistered = false;
            this.trackerInitialized = false;
            this.previousTrackerInitialized = false;
            this.trackingRunning = false;

            // Get a handle to the current object and make sure, that the object
            // doesn't get deleted by the garbage collector. We then use this
            // handle as client data for the native callbacks. This allows us to
            // retrieve the current address of the actual object during the
            // callback execution. GCHandleType.Pinned is not necessary, because we
            // are accessing the address only through the handle object, which gets
            // stored in a global handle table.
            this.gcHandle = GCHandle.Alloc(this);

            CreateLogger();
            CreateWorker();
            RegisterListeners();

            // fire the event at the end of the Awake() function
            OnWorkerCreated?.Invoke();
        }

        private void Awake()
        {
            try
            {
                if (!IsMajorVersionSupported())
                {
                    LogHelper.LogError(
                        "This version of the VisionLib SDK for Unity may not work with the provided native VisionLib SDK version (" +
                        VLSDK.GetVersionString() + ").\n" + "Only major version " +
                        supportedMajorVersion + " is supported.");
                }
                else if (!IsMinorVersionSupported())
                {
                    LogHelper.LogWarning(
                        "This version of the VisionLib SDK for Unity may not work with the provided native VisionLib SDK version (" +
                        VLSDK.GetVersionString() + ").\n" +
                        "The following versions are supported: " + supportedMajorVersion + "." +
                        minimalMinorVersion + ".0 and higher");
                }
            }
            catch (InvalidOperationException)
            {
                LogHelper.LogWarning("Failed to get version strings");
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            string streamingAssetsPath = "file:///android_asset/";
#else
            string streamingAssetsPath = Application.streamingAssetsPath;
#endif
            if (!VLSDK.RegisterScheme("streaming-assets-dir", streamingAssetsPath))
            {
                LogHelper.LogWarning(
                    "Could not register scheme 'streaming-assets-dir' with uri '" +
                    streamingAssetsPath + "'");
            }

            Initialize();
        }

        private void Start()
        {
            LogHelper.LogInfo(
                "VisionLib version: v" + VLSDK.GetVersionString() + " (" +
                VLSDK.GetVersionTimestampString() + ", " + VLSDK.GetVersionHashString() + ")");
        }

        protected virtual void OnDestroy()
        {
            UnregisterListeners();

            try
            {
                lock(this.workerLock)
                {
                    // Release the worker reference (this is necessary, because it
                    // references native resources)
                    this.Worker.Dispose();
                    this.worker = null;

                    // Release the log listener, because we will add a new one during the
                    // next call to Awake
                    this.logger.Dispose();
                    this.logger = null;

                    // Release the handle to the current object
                    this.gcHandle.Free();
                }
            }
            catch (WorkerNotFoundException)
            {
            }
        }

        protected virtual void Update()
        {
            // Log level changed?
            if (this.lastLogLevel != this.logLevel)
            {
                this.logger.SetLogLevel(this.logLevel);
                this.lastLogLevel = this.logLevel;
            }

            // Target FPS changed?
            if (this.lastTargetFPS != this.targetFPS)
            {
                SetFPS(this.targetFPS);
            }

            // Anchor Listeners changed?
            UpdateAnchorTransformListeners();

            ProcessCallbacks();

#if UNITY_2017_1_OR_NEWER
            this.logger.FlushLogBuffer();
#endif
        }

#if !UNITY_EDITOR && (UNITY_WSA_10_0 || UNITY_ANDROID)
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseTrackingInternal();
            }
            else if (this.trackingRunning)
            {
                ResumeTracking();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                PauseTrackingInternal();
            }
            else if (this.trackingRunning)
            {
                ResumeTracking();
            }
        }
#endif

        public LicenseInformation GetLicenseInformation()
        {
            return this.Worker.GetLicenseInformation();
        }

        public static void EmitEvents(Frame frame)
        {
            OnExtrinsicData?.Invoke(frame.extrinsicData);
            OnCameraTransform?.Invoke(frame.cameraTransform);
            OnIntrinsicData?.Invoke(frame.intrinsicData);
            OnImage?.Invoke(frame.image);
            if (frame.calibratedDepthImage != null)
            {
                OnCalibratedDepthImage?.Invoke(frame.calibratedDepthImage);
            }
            OnTrackingStates?.Invoke(frame.trackingState);

            anchorObservableMap.NotifyAll(frame.anchorTransforms);
        }

        public enum ImageStream { None, CameraImage, DebugImage, DepthImage }

        private Dictionary<ImageStream, ImageStreamTexture> streamDictionary =
            new Dictionary<ImageStream, ImageStreamTexture>();

        private void InitializeImageStreams()
        {
            DeinitializeImageStreams();
            this.streamDictionary.Add(ImageStream.CameraImage, new CameraImageStreamTexture());

            // Only add the DebugImageStreamTexture, if a DebugImageListener could be registered
            // DebugImageListeners are removed automatically, whenever the tracker gets destroyed
            if (this.Worker.AddDebugImageListener(
                    dispatchDebugImageCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                this.streamDictionary.Add(ImageStream.DebugImage, new DebugImageStreamTexture());
            }

            this.streamDictionary.Add(ImageStream.DepthImage, new DepthImageStreamTexture());
        }

        private bool warnedAboutMissingDebugImage = false;

        private void DeinitializeImageStreams()
        {
            this.warnedAboutMissingDebugImage = false;
            foreach (var stream in this.streamDictionary)
            {
                stream.Value.DeInit();
            }
            this.streamDictionary.Clear();
            Resources.UnloadUnusedAssets();
        }

        public Texture2D GetStreamTexture(ImageStream streamType)
        {
            ImageStreamTexture stream;
            if (this.streamDictionary.TryGetValue(streamType, out stream))
            {
                return stream.GetTexture();
            }
            if (this.GetTrackingRunning() && streamType != ImageStream.None &&
                !this.warnedAboutMissingDebugImage)
            {
                if (GetTrackerType() == "MultiModelTracker")
                {
                    NotificationHelper.SendWarning(
                        "Multi Model Tracking does currently not support the use of debug images.");
                }
                else
                {
                    string warningMessage =
                        "Accessed stream " + streamType.ToString() +
                        " is currently not available. Please add \"debugLevel\": 1 to the parameters section of the tracker in your .vl file.";
                    NotificationHelper.SendWarning(warningMessage);
                }
                this.warnedAboutMissingDebugImage = true;
            }
            return Texture2D.blackTexture;
        }

        /// <summary>
        /// This function should be used when awaiting a Task created anywhere within the
        /// vlUnitySDK. It will await the Task while treating all VisionLib specific errors which
        /// might arise from calling a command. Using this function also preserves the call stack,
        /// so you will still be able to identify the function which causes the logged error.
        /// </summary>
        /// <param name="task">Task which should be awaited.</param>
        /// <param name="caller">
        /// MonoBehaviour which should be referenced, when selecting error message in the log.
        /// </param>
        public static async void CatchCommandErrors(Task task, MonoBehaviour caller = null)
        {
            try
            {
                await task;
            }
            catch (WorkerCommands.CommandError e)
            {
                var issue = e.GetIssue();
                issue.caller = caller;
                TriggerIssue(issue);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}