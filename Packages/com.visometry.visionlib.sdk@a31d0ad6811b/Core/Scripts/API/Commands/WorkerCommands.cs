using System;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// <summary>
    ///  Commands for communicating with the Worker.
    /// </summary>
    /// @ingroup API
    public static class WorkerCommands
    {
        [Serializable]
        public class CommandBase
        {
            public string name;
            public CommandBase(string name)
            {
                this.name = name;
            }
        }
        public class JsonAndBinaryCommandBase
        {
            [Serializable]
            public class DescriptionBase
            {
                public string name;
                public DescriptionBase(string name)
                {
                    this.name = name;
                }
            }

            public string jsonString;
            public IntPtr binaryData;
            public UInt32 binaryDataSize;

            public JsonAndBinaryCommandBase(
                DescriptionBase commandDescription,
                IntPtr binaryData,
                UInt32 binaryDataSize)
            {
                this.jsonString = JsonHelper.ToJson(commandDescription);
                this.binaryData = binaryData;
                this.binaryDataSize = binaryDataSize;
            }
        }

        /// <summary>
        ///  Creates a tracker from a vl-file.
        /// </summary>
        public static async Task<TrackerInfo> CreateTrackerAsync(Worker worker, string trackingFile)
        {
            return await worker.PushCommandAsync<TrackerInfo>(new CreateTrackerCmd(trackingFile));
        }

        /// <summary>
        ///  Creates a tracker from a vl-string.
        /// </summary>
        public static async Task<TrackerInfo>
            CreateTrackerAsync(Worker worker, string trackingConfiguration, string fakeFileName)
        {
            return await worker.PushCommandAsync<TrackerInfo>(
                new CreateTrackerFromStringCmd(trackingConfiguration, fakeFileName));
        }

        /// <summary>
        ///  Sets the target number of frames per seconds of the tracking thread.
        /// </summary>
        public static async Task SetTargetFPSAsync(Worker worker, int targetFPS)
        {
            await worker.PushCommandAsync(new SetTargetFpsCmd(targetFPS));
        }

        /// <summary>
        ///  Gets the current value of a certain attribute.
        /// </summary>
        public static async Task<GetAttributeResult>
            GetAttributeAsync(Worker worker, string attributeName)
        {
            return await worker.PushCommandAsync<GetAttributeResult>(
                new GetAttributeCmd(attributeName));
        }

        /// <summary>
        ///  Sets the value of a certain attribute.
        /// </summary>
        public static async
            Task SetAttributeAsync(Worker worker, string attributeName, string attributeValue)
        {
            await worker.PushCommandAsync(new SetAttributeCmd(attributeName, attributeValue));
        }

        /// <summary>
        ///  Starts the tracking.
        /// </summary>
        public static async Task RunTrackingAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("runTracking"));
        }

        /// <summary>
        ///  Stops the tracking.
        /// </summary>
        public static async Task PauseTrackingAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("pauseTracking"));
        }

        /// <summary>
        ///  Set the device orientation.
        /// </summary>
        public static async Task SetDeviceOrientationAsync(Worker worker, int mode)
        {
            await worker.PushCommandAsync(new SetDeviceOrientationCmd(mode));
        }

        [Serializable]
        private class CreateTrackerCmd : CommandBase
        {
            private static readonly string defaultName = "createTracker";

            [Serializable]
            public class Param
            {
                public string uri;
            }

            public Param param = new Param();
            public CreateTrackerCmd(string trackingFile) : base(defaultName)
            {
                this.param.uri = trackingFile;
            }
        }

        [Serializable]
        private class CreateTrackerFromStringCmd : CommandBase
        {
            private static readonly string defaultName = "createTrackerFromString";

            [Serializable]
            public class Param
            {
                public string str;
                public string fakeFilename;
            }

            public Param param = new Param();
            public CreateTrackerFromStringCmd(string trackingConfiguration, string fakeFileName) :
                base(defaultName)
            {
                this.param.str = trackingConfiguration;
                this.param.fakeFilename = fakeFileName;
            }
        }

        [Serializable]
        private class SetTargetFpsCmd : CommandBase
        {
            private static readonly string defaultName = "setTargetFPS";

            [Serializable]
            public class Param
            {
                public int targetFPS;
            }
            public Param param = new Param();

            public SetTargetFpsCmd(int fps) : base(defaultName)
            {
                this.param.targetFPS = fps;
            }
        }

        [Serializable]
        private class GetAttributeCmd : CommandBase
        {
            private static readonly string defaultName = "getAttribute";

            [Serializable]
            public class Param
            {
                public string att;
            }
            public Param param = new Param();
            public GetAttributeCmd(string attributeName) : base(defaultName)
            {
                this.param.att = attributeName;
            }
        }

        [Serializable]
        private class SetAttributeCmd : CommandBase
        {
            private static readonly string defaultName = "setAttribute";

            [Serializable]
            public struct Param
            {
                public string att;
                public string val;
            }

            public Param param = new Param();
            public SetAttributeCmd(string attributeName, string attributeValue) : base(defaultName)
            {
                this.param.att = attributeName;
                this.param.val = attributeValue;
            }
        }

        [Serializable]
        private class SetDeviceOrientationCmd : CommandBase
        {
            private static readonly string defaultName = "setDeviceOrientation";

            [Serializable]
            public class Param
            {
                public int mode;
            }

            public Param param = new Param();
            public SetDeviceOrientationCmd(int mode) : base(defaultName)
            {
                this.param.mode = mode;
            }
        }

        // Return types

        /// <summary>
        ///  Error returned from Worker.JsonStringCallback.
        /// </summary>
        [Serializable]
        public class CommandError : System.Exception
        {
            public int errorCode;
            public string commandName;
            public string info;
            public string message;

            public bool IsCanceled()
            {
                return errorCode == 700;
            }

            public Issue GetIssue()
            {
                Issue issue = new Issue();
                issue.commandName = commandName;
                issue.code = errorCode;
                issue.info = info;
                issue.message = message;
                issue.level = Issue.IssueType.Error;
                return issue;
            }
        }

        /// <summary>
        ///  Result of GetAttributeCmd.
        /// </summary>
        [Serializable]
        public struct GetAttributeResult
        {
            public string value;
        }
    }
}