using System;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/General Settings")]
    public class GeneralSettings : TrackingManagerReference
    {
        public VLSDK.LogLevel logLevel = VLSDK.LogLevel.Warning;
        private VLSDK.LogLevel previousLogLevel;

        private void Awake()
        {
            ApplyLogLevel();
            this.previousLogLevel = this.logLevel;
        }

        private void Update()
        {
            if (this.logLevel != this.previousLogLevel)
            {
                ApplyLogLevel();
                this.previousLogLevel = this.logLevel;
            }
        }

        private void ApplyLogLevel()
        {
            this.trackingManager.logLevel = this.logLevel;
            NotificationHelper.logLevel = this.logLevel;
            LogHelper.logLevel = this.logLevel;
        }
    }
}
