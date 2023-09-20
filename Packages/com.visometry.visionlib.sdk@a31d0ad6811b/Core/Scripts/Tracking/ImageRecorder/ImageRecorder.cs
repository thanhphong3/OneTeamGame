﻿using System;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;
using UnityEngine;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    /// ImageRecorder stores the result of a calibration process.
    /// </summary>
    /// @ingroup Core
    [Serializable]
    [AddComponentMenu("VisionLib/Core/Image Recorder")]
    public class ImageRecorder : TrackingManagerReference
    {
        public async Task PauseRecordingAsync()
        {
            await ImageRecorderCommands.PauseAsync(this.worker);
            NotificationHelper.SendInfo("Recording paused");
        }

        /// <summary>
        /// Pauses the recording of new images.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void PauseRecording()
        {
            TrackingManager.CatchCommandErrors(PauseRecordingAsync(), this);
        }

        public async Task ResumeRecordingAsync()
        {
            await ImageRecorderCommands.StartAsync(this.worker);
            NotificationHelper.SendInfo("Recording resumed");
        }

        /// <summary>
        /// Starts/Resumes the recording of new images.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void ResumeRecording()
        {
            TrackingManager.CatchCommandErrors(ResumeRecordingAsync(), this);
        }

        public async Task ResetRecordingAsync()
        {
            await ImageRecorderCommands.ResetAsync(this.worker);
            NotificationHelper.SendInfo("Recording reset");
        }

        /// <summary>
        /// Restarts the recording.
        /// Depending on your parameter it either overwrites the previous recording or starts the
        /// recording of a new image sequence.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void ResetRecording()
        {
            TrackingManager.CatchCommandErrors(ResetRecordingAsync(), this);
        }
    }
}
