using System;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;
using UnityEngine;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  Class providing functions to use the internal memory of uEye cameras.
    ///  You can use them to save or load your own configurations. Additionally,
    ///  provides the possibility to configure your uEye camera with the IDS camera
    ///  manager and load your settings in VisionLib.
    /// </summary>
    [AddComponentMenu("VisionLib/Core/UEye Camera Parameters")]
    public class UEyeCameraParameters : TrackingManagerReference
    {
        public async Task LoadParametersFromEEPROMAsync()
        {
            await UEyeCameraCommands.LoadParametersFromEEPROMAsync(this.worker);
            NotificationHelper.SendInfo("Loaded UEye parameters from EEPROM");
        }

        /// <summary>
        ///  Load uEye camera settings form internal memory.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void LoadParametersFromEEPROM()
        {
            TrackingManager.CatchCommandErrors(LoadParametersFromEEPROMAsync(), this);
        }

        public async Task SaveParametersToEEPROMAsync()
        {
            await UEyeCameraCommands.SaveParametersToEEPROMAsync(this.worker);
            NotificationHelper.SendInfo("Saved UEye parameters to EEPROM");
        }

        /// <summary>
        ///  Save uEye camera settings to internal memory.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void SaveParametersToEEPROM()
        {
            TrackingManager.CatchCommandErrors(SaveParametersToEEPROMAsync(), this);
        }

        public async Task ResetParametersToDefaultAsync()
        {
            await UEyeCameraCommands.ResetParametersToDefaultAsync(this.worker);
            NotificationHelper.SendInfo("Reset UEye parameters to default values");
        }

        /// <summary>
        ///  Reset uEye camera parameters to default.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void ResetParametersToDefault()
        {
            TrackingManager.CatchCommandErrors(ResetParametersToDefaultAsync(), this);
        }
    }
}
