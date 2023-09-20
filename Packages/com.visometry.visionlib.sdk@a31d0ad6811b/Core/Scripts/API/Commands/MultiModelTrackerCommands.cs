using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.API.Native;
using static Visometry.VisionLib.SDK.Core.API.WorkerCommands;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// <summary>
    ///  Commands for communicating with the multi-model tracker.
    /// </summary>
    /// @ingroup API
    public class MultiModelTrackerCommands : ModelTrackerCommands
    {
        [Serializable]
        public class AnchorAttribute
        {
            public string anchor;
            public string value;

            public AnchorAttribute(string anchorName, string value)
            {
                this.value = value;
                this.anchor = anchorName;
            }
        }

        /// <summary>
        ///  Enables an anchor
        /// </summary>
        public static async Task EnableAnchorAsync(Worker worker, string anchorName)
        {
            await worker.PushCommandAsync(new MultiModelTrackerCommand("enableAnchor", anchorName));
        }

        /// <summary>
        ///  Disables an anchor
        /// </summary>
        public static async Task DisableAnchorAsync(Worker worker, string anchorName)
        {
            await worker.PushCommandAsync(
                new MultiModelTrackerCommand("disableAnchor", anchorName));
        }

        /// <summary>
        ///  Performs a Hard Reset to a specific anchor
        /// </summary>
        public static async Task AnchorResetHardAsync(Worker worker, string anchorName)
        {
            await worker.PushCommandAsync(new AnchorCommand(anchorName, "resetHard"));
        }

        public static async Task<InitPose> AnchorGetInitPoseAsync(Worker worker, string anchorName)
        {
            return await worker.PushCommandAsync<InitPose>(
                new AnchorCommand(anchorName, "getInitPose"));
        }

        /// <summary>
        ///  Performs a SetWorkSpaceCommand to a specific anchor
        /// </summary>
        public static async Task AnchorSetWorkSpaceAsync(
            Worker worker,
            string anchorName,
            WorkSpace.Configuration config)
        {
            await worker.PushCommandAsync(new AnchorSetWorkSpacesCommand(anchorName, config));
        }

        public static async Task AnchorAddModelAsync(
            Worker worker,
            string anchorName,
            ModelProperties modelProperties)
        {
            await worker.PushCommandAsync(new AnchorAddModelCommand(anchorName, modelProperties));
        }

        [System.Obsolete(
            "AnchorSetModelPropertyAsync is obsolete. Please use the specific AnchorSetModelPropertyAsync function instead.")]
        public static async Task
            AnchorSetModelPropertyAsync(
                Worker worker,
                string anchorName,
                ModelProperty property,
                string name,
                bool value)
        {
            switch (property)
            {
                case ModelProperty.Enabled:
                {
                    await AnchorSetModelPropertyEnabledAsync(worker, anchorName, name, value);
                    return;
                }
                case ModelProperty.Occluder:
                {
                    await AnchorSetModelPropertyOccluderAsync(worker, anchorName, name, value);
                    return;
                }
            }
            throw new ArgumentException("ModelProperty has to be either 'Enabled' or 'Occluder'");
        }


        public static async Task AnchorSetModelPropertyEnabledAsync(
            Worker worker,
            string anchorName,
            string name,
            bool value)
        {
            await worker.PushCommandAsync(
                new AnchorSetModelPropertyEnabledCommand(anchorName, name, value));
        }

        public static async Task AnchorSetModelPropertyOccluderAsync(
            Worker worker,
            string anchorName,
            string name,
            bool value)
        {
            await worker.PushCommandAsync(
                new AnchorSetModelPropertyOccluderCommand(anchorName, name, value));
        }

        public static async Task AnchorSetModelPropertyURIAsync(
            Worker worker,
            string anchorName,
            string name,
            string uri)
        {
            await worker.PushCommandAsync(
                new AnchorSetModelPropertyURICommand(anchorName, name, uri));
        }

        /// <summary>
        ///  Sets an attribute of a specific anchor
        /// </summary>
        public static async Task AnchorSetAttributeAsync(
            Worker worker,
            string attributeName,
            List<AnchorAttribute> anchorValueList)
        {
            await worker.PushCommandAsync(
                new AnchorSetAttributeCommand(attributeName, anchorValueList));
        }

        [Serializable]
        private class MultiModelTrackerCommand : CommandBase
        {
            [Serializable]
            public class Param
            {
                public string anchorName;
            }
            public Param param = new Param();

            public MultiModelTrackerCommand(string commandName, string anchorName) :
                base(commandName)
            {
                this.param.anchorName = anchorName;
            }
        }

        [Serializable]
        private class AnchorCommand : CommandBase
        {
            [Serializable]
            public class Param
            {
                public string anchorName;
                public CommandBase content;
            }
            public Param param = new Param();

            public AnchorCommand(string anchorName, string commandName) : base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new CommandBase(commandName);
            }
        }

        [Serializable]
        private class AnchorAddModelCommand : CommandBase
        {
            public Param param = new Param();

            [Serializable]
            public struct Param
            {
                public string anchorName;
                public AddModelCmd content;
            }

            public AnchorAddModelCommand(
                string anchorName,
                ModelProperties properties) :
                base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new AddModelCmd(properties);
            }
        }

        [Serializable]
        private class AnchorSetWorkSpacesCommand : CommandBase
        {
            public Param param = new Param();

            [Serializable]
            public struct Param
            {
                public string anchorName;
                public SetWorkSpacesCmd content;
            }

            public AnchorSetWorkSpacesCommand(string anchorName, WorkSpace.Configuration config) :
                base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new SetWorkSpacesCmd(config);
            }
        }

        [Serializable]
        private class AnchorSetModelPropertyEnabledCommand : CommandBase
        {
            public Param param = new Param();

            [Serializable]
            public struct Param
            {
                public string anchorName;
                public SetModelPropertyEnabledCmd content;
            }

            public AnchorSetModelPropertyEnabledCommand(
                string anchorName,
                string name,
                bool enabled) :
                base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new SetModelPropertyEnabledCmd(name, enabled);
            }
        }

        [Serializable]
        private class AnchorSetModelPropertyOccluderCommand : CommandBase
        {
            public Param param = new Param();

            [Serializable]
            public struct Param
            {
                public string anchorName;
                public SetModelPropertyOccluderCmd content;
            }

            public AnchorSetModelPropertyOccluderCommand(
                string anchorName,
                string name,
                bool occluder) :
                base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new SetModelPropertyOccluderCmd(name, occluder);
            }
        }

        [Serializable]
        private class AnchorSetModelPropertyURICommand : CommandBase
        {
            public Param param = new Param();

            [Serializable]
            public struct Param
            {
                public string anchorName;
                public SetModelPropertyURICmd content;
            }

            public AnchorSetModelPropertyURICommand(
                string anchorName,
                string name,
                string uri) :
                base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new SetModelPropertyURICmd(name, uri);
            }
        }

        [Serializable]
        private class AnchorSetAttributeCommand : CommandBase
        {
            public Param param = new Param();

            [Serializable]
            public struct Param
            {
                public string name;
                public AnchorAttribute[] values;
            }

            public AnchorSetAttributeCommand(
                string attributeName,
                List<AnchorAttribute> anchorValueList) :
                base("setAttributeSeparately")
            {
                this.param.name = attributeName;
                this.param.values = anchorValueList.ToArray();
            }
        }

        [Serializable]
        protected class BinaryAnchorCommandDescription : JsonAndBinaryCommandBase.DescriptionBase
        {
            [Serializable]
            public struct Param
            {
                public string anchorName;
                public CommandBase content;
            }

            public Param param = new Param();

            public BinaryAnchorCommandDescription(string anchorName, string commandName) :
                base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new CommandBase(commandName);
            }
        }

        [Serializable]
        private class AnchorSetGlobalObjectPoseCommand : CommandBase
        {
            public Param param = new Param();

            [Serializable]
            public struct Param
            {
                public string anchorName;
                public SetGlobalObjectPoseCmd content;
            }

            public AnchorSetGlobalObjectPoseCommand(string anchorName, InitPose initPose) :
                base("anchorCommand")
            {
                this.param.anchorName = anchorName;
                this.param.content = new SetGlobalObjectPoseCmd(initPose);
            }
        }

        /// <summary>
        ///  Sets the global object pose of the given anchor
        /// </summary>
        public static async Task SetGlobalObjectPoseAsync(Worker worker, string anchorName, InitPose initPose)
        {
            await worker.PushCommandAsync(
                new AnchorSetGlobalObjectPoseCommand(anchorName, initPose));
        }
    }
}