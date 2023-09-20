using UnityEngine;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  The ModelTracker contains all functions, which are specific
    ///  for the ModelTracker.
    /// </summary>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Model Tracker")]
    public class ModelTracker : TrackingManagerReference
    {
        /// <summary>
        ///  Sets the modelURI to a new value and thus loads a new model.
        /// </summary>
        /// <param name="modelURI">
        ///  URI of the model, which should be used for tracking.
        /// </param>
        public async Task SetModelURIAsync(string modelURI)
        {
            await WorkerCommands.SetAttributeAsync(this.worker, "modelURI", modelURI);
            NotificationHelper.SendInfo("Set model URI: " + modelURI);
        }

        /// <summary>
        ///  Sets the modelURI to a new value and thus loads a new model.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="modelURI">
        ///  URI of the model, which should be used for tracking.
        /// </param>
        public void SetModelURI(string modelURI)
        {
            TrackingManager.CatchCommandErrors(SetModelURIAsync(modelURI), this);
        }

        public async Task ResetTrackingHardAsync()
        {
            await ModelTrackerCommands.ResetHardAsync(this.worker);
#pragma warning disable CS0618 // OnTrackerResetHard is obsolete
            TrackingManager.InvokeOnTrackerResetHard();
#pragma warning restore CS0618 // OnTrackerResetHard is obsolete
            NotificationHelper.SendInfo("Tracker reset");
        }

        /// <summary>
        ///  Reset the tracking and all keyframes.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void ResetTrackingHard()
        {
            TrackingManager.CatchCommandErrors(ResetTrackingHardAsync(), this);
        }

        public async Task ResetTrackingSoftAsync()
        {
            await ModelTrackerCommands.ResetSoftAsync(this.worker);
#pragma warning disable CS0618 // OnTrackerResetSoft is obsolete
            TrackingManager.InvokeOnTrackerResetSoft();
#pragma warning restore CS0618 // OnTrackerResetSoft is obsolete
            NotificationHelper.SendInfo("Tracker reset init pose");
        }

        /// <summary>
        ///  Reset the tracking.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void ResetTrackingSoft()
        {
            TrackingManager.CatchCommandErrors(ResetTrackingSoftAsync(), this);
        }

        public async Task WriteInitDataAsync(string filePrefix = null)
        {
            await ModelTrackerCommands.WriteInitDataAsync(this.worker, filePrefix);
            NotificationHelper.SendInfo("Init data written");
        }

        /// <summary>
        ///  Write the captured initialization data as file to custom location
        ///  with custom name.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <remarks>
        ///  In order to avoid having to use a different file path for each
        ///  platform, the "local-storage-dir" scheme can be used as file prefix.
        ///  This scheme points to different locations depending on the platform:
        ///  * Windows: Current users home directory
        ///  * MacOS: Current users document directory
        ///  * iOS / Android: The current applications document directory
        /// </remarks>
        /// <param name="filePrefix">
        ///  Will be used as filename and path. A time stamp and the file
        ///  extension will be appended automatically. A plausible value could be
        ///  for example "local-storage-dir:MyInitData_".
        /// </param>
        public void WriteInitData(string filePrefix = null)
        {
            TrackingManager.CatchCommandErrors(WriteInitDataAsync(filePrefix), this);
        }

        public async Task ReadInitDataAsync(string uri)
        {
            await ModelTrackerCommands.ReadInitDataAsync(this.worker, uri);
            NotificationHelper.SendInfo("Init data read.");
        }

        /// <summary>
        ///  Loads the captured initialization data as file from a custom location.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <remarks>
        ///  In order to load init data at best use a static uri. A common way is for each
        ///  platform, is using  "local-storage-dir" scheme which can be used as file prefix.
        ///  This scheme points to different locations depending on the platform:
        ///  * Windows: Current users home directory
        ///  * MacOS: Current users document directory
        ///  * iOS / Android: The current applications document directory
        /// </remarks>
        /// <param name="uri">
        ///  Will be used as filename and path. A time stamp and the file
        ///  extension will be appended automatically. A plausible value could be
        ///  for example "local-storage-dir:MyInitData_".
        ///  </param>
        public void ReadInitData(string uri)
        {
            TrackingManager.CatchCommandErrors(ReadInitDataAsync(uri), this);
        }

        public async Task ResetInitDataAsync()
        {
            await ModelTrackerCommands.ResetInitDataAsync(this.worker);
            NotificationHelper.SendInfo("Init data reset.");
        }

        /// <summary>
        ///  Reset the offline initialization data.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <remarks>
        ///  In order to reset the initialization data loaded at the beginning this function  can be
        ///  called. The init data learned on the fly, will still be maintained and can be reset by
        ///  issuing a hard reset.
        /// </remarks>
        public void ResetInitData()
        {
            TrackingManager.CatchCommandErrors(ResetInitDataAsync(), this);
        }

        private async Task AddModelAsync(
            string modelName,string modelURI, bool enabled, bool occluder)
        {
            ModelProperties modelProperties =
                new ModelProperties(modelName, modelURI, enabled, occluder);

            await ModelTrackerCommands.AddModelAsync(this.worker, modelProperties);
        }

        public void AddModel(
            string modelName, string modelURI, bool enabled = true, bool occluder = false)
        {
            TrackingManager.CatchCommandErrors(
                AddModelAsync(modelName, modelURI, enabled, occluder), this);
        }

        public async Task SetModelPropertyEnabledAsync(string name, bool state)
        {
            await ModelTrackerCommands.SetModelPropertyEnabledAsync(
                this.worker, name, state);
            NotificationHelper.SendInfo("Set model property enabled to " + state);
        }

        /// <summary>
        /// Enables/Disables a specific model in the current tracker.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="name">Name of the model</param>
        /// <param name="state">Enabled (true/false)</param>
        public void SetModelPropertyEnabled(string name, bool state)
        {
            TrackingManager.CatchCommandErrors(SetModelPropertyEnabledAsync(name, state), this);
        }

        public async Task SetModelPropertyOccluderAsync(string name, bool state)
        {
            await ModelTrackerCommands.SetModelPropertyOccluderAsync(
                this.worker, name, state);
            NotificationHelper.SendInfo("Set model property occluder to " + state);
        }

        /// <summary>
        /// Sets a specific model as occluder in the current tracker.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="name">Name of the model</param>
        /// <param name="state">Occluder (true/false)</param>
        public void SetModelPropertyOccluder(string name, bool state)
        {
            TrackingManager.CatchCommandErrors(SetModelPropertyOccluderAsync(name, state), this);
        }

        public async Task SetModelPropertyURIAsync(string name, string uri)
        {
            await ModelTrackerCommands.SetModelPropertyURIAsync(
                this.worker, name, uri);
            NotificationHelper.SendInfo("Set model property uri to " + uri);
        }

        /// <summary>
        /// Loads a specific model for the current tracker, which is specified by an uri.
        /// This will remove all other models.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="name">Name of the model</param>
        /// <param name="uri">Path to the model file</param>
        public void SetModelPropertyURI(string name, string uri)
        {
            TrackingManager.CatchCommandErrors(SetModelPropertyURIAsync(name, uri), this);
        }

        public Task<ModelPropertiesStructure> GetModelPropertiesAsync()
        {
            return ModelTrackerCommands.GetModelPropertiesAsync(this.worker);
        }

        public async Task RemoveModelAsync(string name)
        {
            await ModelTrackerCommands.RemoveModelAsync(this.worker, name);
            NotificationHelper.SendInfo("Model " + name + " removed");
        }

        /// <summary>
        /// Removes a specific model from the current tracker.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="name">Name of the model</param>
        public void RemoveModel(string name)
        {
            TrackingManager.CatchCommandErrors(RemoveModelAsync(name), this);
        }
    }
}
