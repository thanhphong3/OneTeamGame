using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.Core
{
    [AddComponentMenu("VisionLib/Core/UEye Plugin Loader")]
    public class UEyePluginLoader : TrackingManagerReference
    {
        void Start()
        {
            if (!this.worker.LoadPlugin("VideoUEye"))
            {
                LogHelper.LogError("Failed to load uEye plugin");
            }
        }
    }
}
