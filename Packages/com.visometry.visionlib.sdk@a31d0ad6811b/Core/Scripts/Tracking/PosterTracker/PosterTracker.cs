using UnityEngine;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  The PosterTracker contains all functions, which are specific
    ///  for the PosterTracker.
    /// </summary>
    /// @ingroup Core
    [AddComponentMenu("VisionLib/Core/Poster Tracker")]
    public class PosterTracker : TrackingManagerReference
    {
        public async Task ResetTrackingHardAsync()
        {
            await PosterTrackerCommands.ResetHardAsync(this.worker);
#pragma warning disable CS0618 // OnTrackerResetHard is obsolete
            TrackingManager.InvokeOnTrackerResetHard();
#pragma warning restore CS0618 // OnTrackerResetHard is obsolete
            NotificationHelper.SendInfo("Tracker reset");
        }

        /// <summary>
        ///  Reset the tracking and all keyframes.
        /// </summary>
        public void ResetTrackingHard()
        {
            TrackingManager.CatchCommandErrors(ResetTrackingHardAsync(), this);
        }
    }
}
