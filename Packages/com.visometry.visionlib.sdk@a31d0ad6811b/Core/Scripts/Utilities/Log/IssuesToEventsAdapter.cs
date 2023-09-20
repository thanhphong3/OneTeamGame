using UnityEngine;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    /// Provides events that send the type, log and details each time
    /// an issue from the VisionLib SDK occurred.
    /// </summary>
    /// @ingroup Core
    public class IssuesToEventsAdapter
    {
        private const string defaultString = "XXX";

        private static string[] SplitValues(string stringToSplit, string separator, int expectedValues)
        {
            string[] separatedStrings =
                stringToSplit.Split(new string[]{ separator}, System.StringSplitOptions.None);

            string[] result = new string[expectedValues];
            for (int i = 0; i < expectedValues; i++)
            {
                if (i < separatedStrings.Length)
                {
                    result[i] = separatedStrings[i];
                }
                else
                {
                    result[i] = defaultString;
                }
            }
            return result;
        }

        /// <summary>
        ///  Delegate for <see cref="OnIssue"/> events.
        /// </summary>
        public delegate void IssueAction(
            Issue.IssueType issueType,
            string issueLog,
            string issueDetails,
            MonoBehaviour caller);
        /// <summary>
        ///  Event which will send the type, log and details after an issue occurred.
        /// </summary>
        public event IssueAction OnIssue;

        public void RegisterToVLIssues()
        {
            TrackingManager.OnIssueTriggered += EmitIssuesEvent;
        }

        public void UnregisterFromVLIssues()
        {
            TrackingManager.OnIssueTriggered -= EmitIssuesEvent;
        }

        private void EmitIssuesEvent(Issue issue)
        {
            OnIssue?.Invoke(
                issue.level,
                GetIssueMessage(issue),
                GetIssueDetails(issue),
                issue.caller);
        }

        private string GetIssueDetails(Issue issue)
        {
            string result = "Details:\nIssue " + issue.code;
            if (issue.message != "")
            {
                result += "\n" + issue.message;
            }
            return result;
        }

        private static string SplitFileNameAndParameters(string fileNameAndParameters)
        {
            string[] fileNameParametersSplit =
                fileNameAndParameters.Split(new string[]{"?"}, System.StringSplitOptions.None);

            if (fileNameParametersSplit.Length <= 1)
            {
                return "\"" + fileNameAndParameters + "\"";
            }

            string[] parameters = fileNameParametersSplit [1]
                                      .Split(new string[]{"&"}, System.StringSplitOptions.None);

            string returnMessage = "\"" + fileNameParametersSplit[0] + "\"\n\nWith parameters:";

            foreach (string parameter in parameters)
            {
                returnMessage += "\n\"" + parameter + "\"";
            }
            return returnMessage;
        }

        public static string GetIssueMessage(Issue issue)
        {
            switch (issue.code)
            {
                case 1:
                    return "Internal error occurred";
                case 2:
                    return "No calibration available for device:\n\n\"" + issue.info + "\"";
                case 3:
                    return "No calibration available for device specified in the tracking configuration:\n\n\"" +
                           issue.info + "\"";
                case 4:
                    return "No camera found";
                case 5:
                    return "No camera access possible: Camera may be removed, used by another process, or no camera access is granted";
                case 10:
                    return "Unable to load camera calibration database:\n\n\"" + issue.info + "\"";
                case 11:
                    return "Unable to parse camera calibration database:\n\n\"" + issue.info + "\"";
                case 12:
                    return "Failed to add camera calibration database:\n\n\"" + issue.info + "\"";
                case 13:
                    return "Overwriting camera calibration for device:\n\n\"" + issue.info + "\"";
                case 14:
                {
                    string[] deviceIDs = SplitValues(issue.info, "-", 2);
                    return string.Format(
                        "Overwriting camera calibration \"{0}\" by alternative ID \"{1}\"",
                        deviceIDs[0],
                        deviceIDs[1]);
                }
                case 20:
                {
                    string[] deprecatedParameters = SplitValues(issue.info, "-", 2);
                    return string.Format(
                        "Used deprecated parameter \"{0}\"; use \"{1}\" instead",
                        deprecatedParameters[0],
                        deprecatedParameters[1]);
                }
                case 95:
                {
                    return "Failed to parse file because of syntax error:\n\n" + issue.info;
                }
                case 96:
                {
                    return "Failed to write into file:\n\n" +
                           SplitFileNameAndParameters(issue.info);
                }
                case 97:
                    return "Permission has not been set:\n\n\"" + issue.info + "\"";
                case 98:
                {
                    return "Failed to load from file:\n\n" + SplitFileNameAndParameters(issue.info);
                }
                case 99:
                    return "File is not valid:\n\n" + SplitFileNameAndParameters(issue.info);
                case 100:
                    return "Can not load file with extension \"" + issue.info +
                           "\". For example only \".json\" and \".vl\" files are allowed for tracking configuration.";
                case 101:
                    return "License file is not valid:\n\n\"" + issue.info + "\"";
                case 102:
                    return "License expired on " + issue.info;
                case 103:
                    return "License runs exceeded; application has been run " + issue.info +
                           " times, but only 5 runs are allowed";
                case 104:
                    return "Unlicensed model found; please register your model hash at visionlib.com:\n\n\"" +
                           issue.info + "\"";
                case 105:
                    return "Unlicensed hostID found; please register your hostID at visionlib.com:\n\n\"" +
                           issue.info + "\"";
                case 106:
                    return "No license file has been set";
                case 107:
                    return "License can not be used on Platform " + issue.info;
                case 108:
                    return "Models are used which are not registered in the license";
                case 109:
                    return "License file not found:\n\n\"" + issue.info + "\"";
                case 110:
                    return "License only allows versions of VisionLib built before " + issue.info;
                case 111:
                    return "License is bound to a software protection dongle and does not work with the current seat";
                case 112:
                    return "The application uses a feature which is not covered by the current license:\n\n\"" +
                           issue.info + "\"";
                case 113:
                    return "License will expire in " + issue.info + " days";
                case 114:
                    return "Unlicensed bundleID found; Please register your bundleID at visionlib.com:\n\n\"" +
                           issue.info + "\"";
                case 115:
                    return "Number of allowed tracking anchors exceeded. Your license allows the simultaneous usage of " +
                           issue.info + " anchors";
                case 200:
                    return "\"" + issue.info + "\" is not supported on your device.";
                case 300:
                    return "Failed to load model:\n\n\"" + issue.info + "\"";
                case 301:
                    return "Failed to decode model\n\n\"" + issue.info + "\"";
                case 302:
                    return "The metric of you model is implausible. Please check the `metric` parameter. The bounding box dimensions are: " +
                           issue.info;
                case 303:
                    return "The modelName has already been used or is used twice:\n\n\"" +
                           issue.info + "\"";
                case 350:
                    return "Failed to find poster image:\n\n\"" + issue.info + "\"";
                case 351:
                    return "Poster quality is only " + issue.info +
                           "; please use a different poster";
                case 400:
                    return "The setup of the graph failed with an unknown reason";
                case 401:
                    return "Could not find the node with the name " + issue.info;
                case 402:
                    return "The data path doesn't comply with the expected pattern \"nodeName.dataName\":\n\n\"" +
                           issue.info + "\"";
                case 403:
                {
                    string[] infoParts = SplitValues(issue.info, " :: ", 3);
                    return string.Format(
                        "Could not find the input \"{0}\" of the node \"{1}\"\n\nPossible values: {2}",
                        infoParts[1],
                        infoParts[0],
                        infoParts[2]);
                }
                case 404:
                {
                    string[] infoParts = SplitValues(issue.info, " :: ", 3);
                    return string.Format(
                        "Could not find the output \"{0}\" of the node \"{1}\"\n\nPossible values: {2}",
                        infoParts[1],
                        infoParts[0],
                        infoParts[2]);
                }
                case 405:
                    return "There is a cycle in the graph of the tracking configuration, so no order of execution could be determined";
                case 406:
                    return "There was no tracker defined inside the tracking configuration";
                case 407:
                    return "The name \"" + issue.info + "\" has been used for two or more devices";
                case 408:
                    return "The name \"" + issue.info + "\" has been used for two or more trackers";
                case 500:
                    return "The name \"" + issue.info + "\" has been used for two or more anchors";
                case 501:
                    return "No anchor with name \"" + issue.info + "\" has been found";
                case 700:
                    return "The command \"" + issue.commandName + "\" aborted";
                case 701:
                    return string.Format(
                        "The command \"{0}\" is not supported by the current pipeline ({1}).\n\nYou may want to deactivate the {2} or remove it from the scene.",
                        issue.commandName,
                        issue.info,
                        issue.caller.name);
                case 702:
                    return "The command \"" + issue.commandName + "\" could not be executed due to internal problems in VisionLib.SDK.Native: " + issue.info;
                case 703:
                {
                    string[] infoParts = SplitValues(issue.info, " :: ", 2);
                    return string.Format(
                        "The command \"{0}\" could not be executed because parameter \"{1}\" did not fit the required structure: {2}",
                        issue.commandName,
                        infoParts[0],
                        infoParts[1]);
                }
                case 704:
                {
                    string[] infoParts = SplitValues(issue.info, " :: ", 3);
                    return string.Format(
                        "The command \"{0}\" could not be executed because \"{1}\" = \"{2}\" is not supported in the current pipeline ({3})",
                        issue.commandName,
                        infoParts[0],
                        infoParts[1],
                        infoParts[2]);
                }
            }
            return null;
        }
    }
}
