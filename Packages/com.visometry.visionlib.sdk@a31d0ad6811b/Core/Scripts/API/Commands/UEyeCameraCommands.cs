using System;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.API.Native;
using static Visometry.VisionLib.SDK.Core.API.WorkerCommands;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// <summary>
    ///  Commands for communicating with the UEye camera.
    /// </summary>
    /// @ingroup API
    public class UEyeCameraCommands
    {
        /// <summary>
        ///  Load uEye camera settings form internal memory.
        /// </summary>
        public static async Task LoadParametersFromEEPROMAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("imageSource.loadParametersFromEEPROM"));
        }

        /// <summary>
        ///  Save uEye camera settings to internal memory.
        /// </summary>
        public static async Task SaveParametersToEEPROMAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("imageSource.saveParametersToEEPROM"));
        }

        /// <summary>
        ///  Reset uEye camera parameters to default.
        /// </summary>
        public static async Task ResetParametersToDefaultAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("imageSource.resetParametersToDefault"));
        }
    }
}
