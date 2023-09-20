using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.Core
{
    [AddComponentMenu("VisionLib/Core/Azure Kinect Plugin Loader")]
    public class AzureKinectPluginLoader : TrackingManagerReference
    {
        void Start()
        {
            if (!this.worker.LoadPlugin("VideoAzureKinect"))
            {
                LogHelper.LogError("Failed to load Azure Kinect plugin");
            }
        }
    }
}
