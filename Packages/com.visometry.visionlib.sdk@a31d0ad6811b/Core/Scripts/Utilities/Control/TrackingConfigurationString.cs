using System;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/Tracking Configuration String")]
    public class TrackingConfigurationString : TrackingManagerReference
    {
        [Tooltip(
            "Definition of the project-dir path in the tracker configuration. It has to be set relative to `StreamingAssets/VisionLib")]
        public string projectDir = "";

        [TextArea(5, 40)]
        public string trackingConfiguration = null;

        public void StartTracking()
        {
            if (this.trackingConfiguration == null)
            {
                return;
            }
            string baseDir = projectDir;
            if (!String.IsNullOrEmpty(projectDir))
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                baseDir = "file:///android_asset/VisionLib";
#else
                baseDir = PathHelper.CombinePaths(Application.streamingAssetsPath, "VisionLib");
#endif
                if (projectDir.StartsWith("/") || projectDir.StartsWith("\\"))
                {
                    baseDir += projectDir;
                }
                else
                {
                    baseDir = PathHelper.CombinePaths(baseDir, projectDir);
                }
            }

            // Load tracking from inline configuration
            this.trackingManager.StartTrackingFromString(this.trackingConfiguration, baseDir);
        }
    }
}
