using System.Threading.Tasks;
using System;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// <summary>
    ///  Commands for communicating with the BarCode Reader.
    /// </summary>
    /// @ingroup API
    public static class BarCodeReaderCommands
    {
        [Serializable]
        public class BarCodeResult
        {
            public string value;
            public string format;
            public bool valid;
            public int framesSinceRecognition;

            public override string ToString()
            {
                return "{\"" + value + "\" " + format + " " + valid + " " + framesSinceRecognition +
                       "}";
            }
        }

        /// <summary>
        ///  Resets the tracking and all keyframes.
        /// </summary>
        public static async Task<BarCodeResult> GetBarCodeResultAsync(Native.Worker worker)
        {
            return await worker.PushCommandAsync<BarCodeResult>(
                new WorkerCommands.CommandBase("getBarCodeResult"));
        }
    }
}