using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  The WorkSpaceManager collects the WorkSpace definitions from the hierarchy
    ///  and transfers them to VisionLib during runtime.
    ///  If TrackingAnchors are found in the scene, the WorkSpaces will be transferred
    ///  according to their anchor.
    ///  **THIS IS SUBJECT TO CHANGE** Please do not rely on this code in productive environments.
    /// </summary>
    /// <seealso cref="WorkSpace"/>
    /// @ingroup WorkSpace
    [AddComponentMenu("VisionLib/Core/AutoInit/WorkSpace Manager")]
    public class WorkSpaceManager : TrackingManagerReference
    {
        [Tooltip("Start pose learning automatically as soon as the tracker initialized")]
        public bool autoStartLearning = false;
        [Tooltip("Show progress of pose learning")]
        public bool showProgressBar = false;

        private ProgressIndication progressBar;
        private const float progressBarMinValue = 0.0f;
        private const float progressBarMaxValue = 1.0f;
        private int progresBarAutoInitObjects = 1;
        private bool updateProgressBar = false;
        private bool startedLearning = false;
        private enum AutoInitSetupState { INACTIVE, PREPARING, READY }
        private const string DEFAULT_ANCHORNAME = "EmptyDefaultName";

        private async Task LearnWorkSpaceInitDataAsync()
        {
            await ModelTrackerCommands.ResetHardAsync(this.worker);
            await PushWorkSpacesAsync();
            if (this.showProgressBar)
            {
                InitProgressBar();
            }
        }

        /// <summary>
        /// Resets the tracking (hard) and initializes the learning process for AutoInit.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void LearnWorkSpaceInitData()
        {
            TrackingManager.CatchCommandErrors(LearnWorkSpaceInitDataAsync(), this);
        }

        /// <summary>
        /// Search the scene for TrackingAnchors and collect their WorkSpaceConfigurations.
        /// If No TrackingAnchors are found, collect all WorkSpaces and create a
        /// WorkSpaceConfiguration from them.
        /// </summary>
        /// <returns>
        /// Dictionary containing all anchor names and their WorkSpace configurations.
        /// If the scene only tracks a single object, the dictionary will have one entry
        /// with the anchor name "EmptyDefaultName".
        /// </returns>
        private Dictionary<string, API.WorkSpace.Configuration>
            GetWorkSpaceConfigurationsFromScene()
        {
            TrackingAnchor[] trackingAnchors = FindObjectsOfType<TrackingAnchor>();

            if (trackingAnchors.Length > 0)
            {
                return GetWorkSpaceConfigurationsFromAnchors(trackingAnchors);
            }

            var workSpaces = FindObjectsOfType<WorkSpace>();
            if (workSpaces.Length < 1)
            {
                LogHelper.LogError("No WorkSpaces found in scene");
                return null;
            }

            Dictionary<string, API.WorkSpace.Configuration> workSpaceConfigurations =
                new Dictionary<string, API.WorkSpace.Configuration>();

            workSpaceConfigurations.Add(
                DEFAULT_ANCHORNAME, CreateWorkSpaceConfiguration(workSpaces));
            progresBarAutoInitObjects = workSpaceConfigurations.Count();

            return workSpaceConfigurations;
        }

        /// <summary>
        /// For an array of TrackingAnchors, create a WorkSpaceConfiguration from all
        /// WorkSpaces referenced by each anchor and return a dictionary to store them.
        /// </summary>
        /// <param name="trackingAnchors"></param>
        /// <returns>WorkSpaceConfigurations associated by anchorNames</returns>
        private Dictionary<string, API.WorkSpace.Configuration>
            GetWorkSpaceConfigurationsFromAnchors(TrackingAnchor[] trackingAnchors)
        {
            Dictionary<string, API.WorkSpace.Configuration> workSpaceConfigurations =
                new Dictionary<string, API.WorkSpace.Configuration>();

            foreach (TrackingAnchor anchor in trackingAnchors)
            {
                if (workSpaceConfigurations.ContainsKey(anchor.anchorName))
                {
                    LogHelper.LogWarning(
                        "You have multiple anchors with the name `" + anchor.anchorName +
                            "` in your scene. Anchor names must be unique. " +
                            "Only the first anchor with this name will be used.",
                        anchor);
                }
                else
                {
                    if (anchor.workSpaces.Length == 0)
                    {
                        LogHelper.LogWarning("The anchor " + anchor.anchorName + " has no workspace defined.", anchor);
                        continue;
                    }
                    workSpaceConfigurations.Add(
                        anchor.anchorName, CreateWorkSpaceConfiguration(anchor.workSpaces));
                }
            }
            progresBarAutoInitObjects = workSpaceConfigurations.Count();
            return workSpaceConfigurations;
        }

        /// <summary>
        /// Creates a WorkSpace.Configuration from a list of WorkSpaces,
        /// which can be used as a parameter for ModelTrackerCommands.SetWorkSpacesCmd.
        /// </summary>
        /// <returns>Serializable configuration of WorkSpaces</returns>
        private API.WorkSpace.Configuration CreateWorkSpaceConfiguration(WorkSpace[] workSpaces)
        {
            bool useCameraRotation = true;
            var workSpaceDefinitions =
                workSpaces.Select(ws => ws.GetWorkSpaceDefinition(useCameraRotation)).ToList();
            return new API.WorkSpace.Configuration(workSpaceDefinitions);
        }

        /// <summary>
        /// Returns a WorkSpace Configuration
        /// containing all WorkSpaces that are associated with the given anchor.
        /// </summary>
        private API.WorkSpace.Configuration GetWorkSpaceConfigurationByAnchorName(
            Dictionary<string, API.WorkSpace.Configuration> workSpaceConfigurations,
            string anchorName)
        {
            if (workSpaceConfigurations.ContainsKey(anchorName))
            {
                API.WorkSpace.Configuration config;
                workSpaceConfigurations.TryGetValue(anchorName, out config);
                return config;
            }
            else
            {
                LogHelper.LogWarning(
                    "No WorkSpace Configuration for the anchorName `" + anchorName + "` found.");
                return null;
            }
        }

        /// <summary>
        /// Creates the WorkSpace.Configuration and writes it into the given file.
        /// </summary>
        /// <param name="fileName">Path of the file to write the data in.</param>
        /// <remarks>
        ///  <para>
        ///   It's possible to use vlSDK file schemes (e.g. local-storage-dir) here.
        ///  </para>
        /// </remarks>
        public void WriteWorkSpaceConfigurationToFile(
            string anchorName = DEFAULT_ANCHORNAME,
            string fileName = "local-storage-dir:/VisionLib/AutoInit/workspaceConfiguration.json")
        {
            GetWorkSpaceConfigurationByAnchorName(GetWorkSpaceConfigurationsFromScene(), anchorName)
                .WriteToFile(fileName);
        }

        /// <summary>
        /// Pushes the WorkSpace.Configuration of each anchor to the vlSDK.
        /// </summary>
        private async Task PushWorkSpacesAsync()
        {
            Dictionary<string, API.WorkSpace.Configuration> workSpaceConfigurations =
                GetWorkSpaceConfigurationsFromScene();

            if (workSpaceConfigurations == null)
            {
                return;
            }

            List<Task> anchorUpdates = new List<Task>();
            foreach (string anchor in workSpaceConfigurations.Keys)
            {
                var config = GetWorkSpaceConfigurationByAnchorName(workSpaceConfigurations, anchor);

                if (anchor == DEFAULT_ANCHORNAME)
                {
                    anchorUpdates.Add(ModelTrackerCommands.SetWorkSpacesAsync(this.worker, config));
                }
                else
                {
                    anchorUpdates.Add(MultiModelTrackerCommands.AnchorSetWorkSpaceAsync(
                        this.worker, anchor, config));
                }
            }
            await Task.WhenAll(anchorUpdates);
        }

        /// <summary>
        /// Initializes the progress bar to show the learning progress.
        /// </summary>
        private void InitProgressBar()
        {
            if (!this.updateProgressBar)
            {
                this.progressBar = new ProgressIndication(
                    "Preparing AutoInit",
                    progressBarMinValue,
                    progressBarMaxValue,
                    "AutoInitSetupProgress");
            }

            this.updateProgressBar = true;
            this.startedLearning = false;
        }

        /// <summary>
        /// Updates progress value of AutoInit.
        /// </summary>
        /// <param name="state">Current tracking state</param>
        private void UpdateAutoInitSetupProgress(TrackingState state)
        {
            if (!this.updateProgressBar)
            {
                return;
            }

            float progress = 0.0f;
            foreach (TrackingState.TrackingObject obj in state.objects)
            {
                progress += obj._AutoInitSetupProgress;
            }
            this.progressBar.Value = progress / progresBarAutoInitObjects;

            AutoInitSetupState aggregatedSetupState = AggregateAutoInitSetupStates(state.objects);

            if (!this.startedLearning && aggregatedSetupState != AutoInitSetupState.INACTIVE)
            {
                this.startedLearning = true;
            }

            if (this.startedLearning && aggregatedSetupState == AutoInitSetupState.INACTIVE)
            {
                this.progressBar.Abort("Canceled AutoInit Learning");
                this.updateProgressBar = false;
                return;
            }

            if (aggregatedSetupState == AutoInitSetupState.READY)
            {
                this.progressBar.Finish("Completed AutoInit Learning");
                this.updateProgressBar = false;
            }
        }

        private AutoInitSetupState
            AggregateAutoInitSetupStates(TrackingState.TrackingObject[] trackingObjects)
        {
            bool anyPreparing = false;
            bool allInactive = true;

            foreach (TrackingState.TrackingObject obj in trackingObjects)
            {
                string setupState = obj._AutoInitSetupState;

                anyPreparing |= setupState == AutoInitSetupState.PREPARING.ToString();
                allInactive &= setupState == AutoInitSetupState.INACTIVE.ToString();
            }

            return anyPreparing ? AutoInitSetupState.PREPARING :
                (allInactive ? AutoInitSetupState.INACTIVE
                 : AutoInitSetupState.READY);
        }

        private void LearnWorkSpaceInitDataIfAutoStartLearning()
        {
            if (this.autoStartLearning)
            {
                LearnWorkSpaceInitData();
            }
        }

        void OnEnable()
        {
            TrackingManager.OnTrackerInitialized += LearnWorkSpaceInitDataIfAutoStartLearning;
            TrackingManager.OnTrackingStates += UpdateAutoInitSetupProgress;
        }

        void OnDisable()
        {
            TrackingManager.OnTrackerInitialized -= LearnWorkSpaceInitDataIfAutoStartLearning;
            TrackingManager.OnTrackingStates -= UpdateAutoInitSetupProgress;
        }
    }
}
