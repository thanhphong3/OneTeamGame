using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/Bar Code Reader")]
    public class BarCodeReader : TrackingManagerReference
    {
        [Serializable]
        public class OnJustScannedEvent : UnityEvent<string>
        {
        }

        /// <summary>
        ///  Event fired once after the code detection state changed to "found".
        /// </summary>
        public OnJustScannedEvent justScannedEvent = new OnJustScannedEvent();

        /// <summary>
        ///  Event fired once after the code detection state changed to "lost".
        /// </summary>
        public UnityEvent justLostEvent = new UnityEvent();

        private string previousValue = null;
        private int frameThreshold = 20;
        private SingletonTaskExecutor getBarCodeResultExecuter;

        public BarCodeReader()
        {
            getBarCodeResultExecuter = new SingletonTaskExecutor(GetBarCodeResultAsync, this);
        }

        private async Task GetBarCodeResultAsync()
        {
            var result = await BarCodeReaderCommands.GetBarCodeResultAsync(this.worker);
            string currentValue = null;
            if (result.valid && result.framesSinceRecognition <= this.frameThreshold)
            {
                currentValue = result.value;
            }
            if (this.previousValue != currentValue)
            {
                if (currentValue == null)
                {
                    LogHelper.LogInfo("Bar code lost.");
                    this.justLostEvent.Invoke();
                }
                else
                {
                    LogHelper.LogInfo("Bar code found with value: " + currentValue);
                    this.justScannedEvent.Invoke(currentValue);
                }
            }
            this.previousValue = currentValue;
        }

        private void GetBarCodeResult()
        {
            if (this.trackingManager.GetTrackerInitialized())
            {
                getBarCodeResultExecuter.TryExecute();
            }
        }

        void Update()
        {
            this.GetBarCodeResult();
        }
    }
}
