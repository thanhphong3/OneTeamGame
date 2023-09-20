using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Examples
{
    /**
     *  @ingroup Examples
     */
    [AddComponentMenu("VisionLib/Examples/Instruction Panel")]
    internal class InstructionPanel : MonoBehaviour
    {
        private const string baseURI = "https://docs.visionlib.com";
        private const string extension = ".html";

        private const string salesMail = "mailto:sales@visometry.com";

#region Documentation Page Tags

        private const string docuIndex = "index";
        private const string modelTrackerConfig = "model_tracker";

        // Tracking essentials
        private const string trackingEssentials = "tracking_essentials";
        private const string understandingTrackingParameters = "vl_unity_s_d_k__article__understanding_tracking_params";
        private const string imageRecorder = "vl_unity_s_d_k_image_sources2_record";
        private const string cameraCalibration = "vl_unity_s_d_k__camera_calibration";
        private const string mutableTracking = "vl_unity_s_d_k__tutorial__multi_model_usage";
        private const string uEyeCameras = "vl_unity_s_d_k_u_eye_cameras";

        // Unity tutorials
        private const string quickStart = "vl_unity_s_d_k__user_guide_qick_start";
        private const string posterTracking = "vl_unity_s_d_k_poster_tracker_tutorial";
        private const string dynamicTracking = "vl_unity_s_d_k_dynamic_model_tracking";
        private const string autoInit = "vl_unity_s_d_k_auto_init_tutorial";
        private const string multiModel = "vl_unity_s_d_k_multi_model_tutorial";
        private const string arFoundation = "vl_unity_s_d_k__a_r_foundation";

#endregion

        private string GetVersionDirectory()
        {
            // The documentation URI does _not_ contain the version postfix
            // therefor we can _not_ use VLSDK.GetVersionString()
            return "v" + VLSDK.GetMajorVersion() + "." + VLSDK.GetMinorVersion() + "." +
                   VLSDK.GetRevisionVersion();
        }

        private void OnEnable()
        {
            TrackingManager.OnTrackerInitialized += HidePanel;
            TrackingManager.OnTrackerStopped += HidePanel;
        }
        private void OnDisable()
        {
            TrackingManager.OnTrackerInitialized -= HidePanel;
            TrackingManager.OnTrackerStopped -= HidePanel;
        }

        private void HidePanel()
        {
            this.gameObject.SetActive(false);
        }

        private void CombineAndOpenURI(string pageTag)
        {
            string version = GetVersionDirectory();
            string path = PathHelper.CombinePaths(baseURI, version, pageTag + extension);

            Application.OpenURL(path);
        }

        public void OpenSalesMail()
        {
            Application.OpenURL(salesMail);
        }

#region Open Documentation Pages

        public void OpenDocumentation()
        {
            CombineAndOpenURI(docuIndex);
        }

        public void OpenModelTrackerConfig()
        {
            CombineAndOpenURI(modelTrackerConfig);
        }

        public void OpenTrackingEssentials()
        {
            CombineAndOpenURI(trackingEssentials);
        }

        public void OpenUnderstandingTrackingParameters()
        {
            CombineAndOpenURI(understandingTrackingParameters);
        }

        public void OpenImageRecorder()
        {
            CombineAndOpenURI(imageRecorder);
        }

        public void OpenCameraCalibration()
        {
            CombineAndOpenURI(cameraCalibration);
        }

        public void OpenQuickStart()
        {
            CombineAndOpenURI(quickStart);
        }

        public void OpenPosterTracking()
        {
            CombineAndOpenURI(posterTracking);
        }

        public void OpenDynamicTracking()
        {
            CombineAndOpenURI(dynamicTracking);
        }

        public void OpenMutableTracking()
        {
            CombineAndOpenURI(mutableTracking);
        }

        public void OpenAutoInit()
        {
            CombineAndOpenURI(autoInit);
        }

        public void OpenMultiModel()
        {
            CombineAndOpenURI(multiModel);
        }

        public void OpenARFoundation()
        {
            CombineAndOpenURI(arFoundation);
        }

        public void OpenUEye()
        {
            CombineAndOpenURI(uEyeCameras);
        }

#endregion
    }
}
