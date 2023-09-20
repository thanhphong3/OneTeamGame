using System;
using System.Threading.Tasks;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.API.Native;
using static Visometry.VisionLib.SDK.Core.API.WorkerCommands;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// <summary>
    ///  Commands for communicating with the model tracker.
    /// </summary>
    /// @ingroup API
    public class ModelTrackerCommands
    {
        /// <summary>
        ///  Result of GetInitPoseCmd.
        /// </summary>
        [Serializable]
        public struct InitPose
        {
            public float[] t;
            public float[] q;
            public InitPose(Vector4 t, Quaternion q)
            {
                this.t = new float[3] { t.x, t.y, t.z };
                this.q = new float[4] { q.x, q.y, q.z, q.w };
            }

            public InitPose(ModelTransform mt) : this(mt.t, mt.q) {}
        }

        [System.Obsolete(
            "ModelProperty is obsolete. Please use the specific SetModelProperty function instead.")]
        public enum ModelProperty {
            Enabled,
            Occluder
        }

        /// <summary>
        ///  Resets the tracking.
        /// </summary>
        public static async Task ResetSoftAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("resetSoft"));
        }

        /// <summary>
        ///  Resets the tracking and all keyframes.
        /// </summary>
        public static async Task ResetHardAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("resetHard"));
        }

        /// <summary>
        ///  Get the initial pose.
        /// </summary>
        public static async Task<InitPose> GetInitPoseAsync(Worker worker)
        {
            return await worker.PushCommandAsync<InitPose>(new CommandBase("getInitPose"));
        }

        /// <summary>
        ///  Set the initial pose.
        /// </summary>
        public static async Task SetInitPoseAsync(Worker worker, InitPose initPose)
        {
            await worker.PushCommandAsync(new SetInitPoseCmd(initPose));
        }

        /// <summary>
        ///  Set the global pose of the object to track.
        /// </summary>
        public static async Task SetGlobalObjectPoseAsync(Worker worker, InitPose objectPose)
        {
            await worker.PushCommandAsync(new SetGlobalObjectPoseCmd(objectPose));
        }

        /// <summary>
        ///  Write init data to default location (filePrefix == null) or custom location.
        /// </summary>
        public static async Task WriteInitDataAsync(Worker worker, string filePrefix = null)
        {
            if (filePrefix == null)
            {
                await worker.PushCommandAsync(new CommandBase("writeInitData"));
                return;
            }
            await worker.PushCommandAsync(new WriteInitDataWithPrefixCmd(filePrefix));
        }

        /// <summary>
        ///  Read init data from custom location with custom file name.
        /// </summary>
        public static async Task ReadInitDataAsync(Worker worker, string filePrefix)
        {
            await worker.PushCommandAsync(new ReadInitDataWithPrefixCmd(filePrefix));
        }

        /// <summary>
        ///  Reset Offline init data
        /// </summary>
        public static async Task ResetInitDataAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("resetInitData"));
        }

        public static async Task AddModelAsync(Worker worker, ModelProperties properties)
        {
            await worker.PushCommandAsync(new AddModelCmd(properties));
        }

        public static async Task<ModelPropertiesStructure> GetModelPropertiesAsync(Worker worker)
        {
            return await worker.PushCommandAsync<ModelPropertiesStructure>(
                new CommandBase("getModelProperties"));
        }

        public static async Task RemoveModelAsync(Worker worker, string modelName)
        {
            await worker.PushCommandAsync(new RemoveModelCmd(modelName));
        }

        [System.Obsolete(
            "SetModelPropertyAsync is obsolete. Please use the specific SetModelProperty function instead.")]
        public static async Task
            SetModelPropertyAsync(Worker worker, ModelProperty property, string name, bool value)
        {
            switch (property)
            {
                case ModelProperty.Enabled:
                {
                    await SetModelPropertyEnabledAsync(worker, name, value);
                    return;
                }
                case ModelProperty.Occluder:
                {
                    await SetModelPropertyOccluderAsync(worker, name, value);
                    return;
                }
            }
            throw new ArgumentException("ModelProperty has to be either 'Enabled' or 'Occluder'");
        }

        public static async Task
            SetModelPropertyEnabledAsync(Worker worker, string name, bool value)
        {
            await worker.PushCommandAsync(new SetModelPropertyEnabledCmd(name, value));
        }

        public static async Task
            SetModelPropertyOccluderAsync(Worker worker, string name, bool value)
        {
            await worker.PushCommandAsync(new SetModelPropertyOccluderCmd(name, value));
        }

        public static async Task SetModelPropertyURIAsync(Worker worker, string name, string uri)
        {
            await worker.PushCommandAsync(new SetModelPropertyURICmd(name, uri));
        }

        public static async
            Task SetMultipleModelPropertiesAsync(Worker worker, ModelDataDescriptorList models)
        {
            await worker.PushCommandAsync(new SetMultipleModelPropertiesCmd(models));
        }

        public static async Task Set1DRotationConstraintAsync(
            Worker worker,
            Vector3 worldUpVector,
            Vector3 modelUpVector,
            Vector3 modelCenter)
        {
            await worker.PushCommandAsync(
                new Set1DRotationConstraintCommand(worldUpVector, modelUpVector, modelCenter));
        }

        public static async Task DisableConstraintAsync(Worker worker)
        {
            await worker.PushCommandAsync(new CommandBase("disableConstraints"));
        }

        /// <summary>
        ///  Set the WorkSpaces for AutoInit.
        /// </summary>
        public static async Task SetWorkSpacesAsync(Worker worker, WorkSpace.Configuration config)
        {
            await worker.PushCommandAsync(new SetWorkSpacesCmd(config));
        }

        public static async Task<ModelDeserializationResultList> AddModelDataAsync(
            Worker worker,
            ModelDataDescriptorList models,
            IntPtr data,
            UInt32 dataSize)
        {
            return await worker.PushCommandAsync<ModelDeserializationResultList>(
                new JsonAndBinaryCommandBase(new AddModelDataCmd(models), data, dataSize));
        }

        /// <summary>
        ///  Set the global coordinate system of Unity to be used in the vlSDK
        /// </summary>
        public static async Task
            SetGlobalCoordinateSystemAsync(Worker worker, IntPtr nativeISpatialCoordinateSystemPtr)
        {
            await worker.PushCommandAsync(new JsonAndBinaryCommandBase(
                new JsonAndBinaryCommandBase.DescriptionBase("setGlobalCoordinateSystem"),
                nativeISpatialCoordinateSystemPtr,
                0));
        }

        [Serializable]
        protected class SetInitPoseCmd : CommandBase
        {
            private static readonly string defaultName = "setInitPose";

            public InitPose param;
            public SetInitPoseCmd(InitPose initPose) : base(defaultName)
            {
                this.param = initPose;
            }
        }

        [Serializable]
        protected class AddModelCmd : CommandBase
        {
            private static readonly string defaultName = "addModel";

            public ModelProperties param;

            public AddModelCmd(ModelProperties properties) : base(defaultName)
            {
                this.param = properties;
            }
        }

        [Serializable]
        protected class WriteInitDataWithPrefixCmd : CommandBase
        {
            private static readonly string defaultName = "writeInitData";
            [Serializable]
            public class Param
            {
                public string uri;
            }
            public Param param = new Param();
            public WriteInitDataWithPrefixCmd(string filePrefix) : base(defaultName)
            {
                this.param.uri = filePrefix;
            }
        }

        [Serializable]
        protected class ReadInitDataWithPrefixCmd : CommandBase
        {
            private static readonly string defaultName = "readInitData";
            [Serializable]
            public class Param
            {
                public string uri;
            }
            public Param param = new Param();
            public ReadInitDataWithPrefixCmd(string filePrefix) : base(defaultName)
            {
                this.param.uri = filePrefix;
            }
        }

        [Serializable]
        protected class RemoveModelCmd : CommandBase
        {
            private static readonly string defaultName = "removeModel";

            [Serializable]
            public class Param
            {
                public string modelName;
            }
            public Param param = new Param();

            public RemoveModelCmd(string modelName) : base(defaultName)
            {
                this.param.modelName = modelName;
            }
        }

        [Serializable]
        protected class SetWorkSpacesCmd : CommandBase
        {
            private static readonly string defaultName = "setWorkSpaces";

            public WorkSpace.Configuration param;

            public SetWorkSpacesCmd(WorkSpace.Configuration config) : base(defaultName)
            {
                this.param = config;
            }
        }

        [Serializable]
        protected class SetMultipleModelPropertiesCmd : CommandBase
        {
            public ModelDataDescriptorList param = new ModelDataDescriptorList();

            public SetMultipleModelPropertiesCmd(ModelDataDescriptorList models) :
                base("setMultipleModelProperties")
            {
                this.param = models;
            }
        }

        [Serializable]
        protected class SetModelPropertyEnabledCmd : CommandBase
        {
            [Serializable]
            public struct Param
            {
                public string name;
                public bool enabled;
            }

            public Param param;
            public SetModelPropertyEnabledCmd(string name, bool enable) : base("setModelProperties")
            {
                this.param.name = name;
                this.param.enabled = enable;
            }
        }

        [Serializable]
        protected class SetModelPropertyOccluderCmd : CommandBase
        {
            [Serializable]
            public struct Param
            {
                public string name;
                public bool occluder;
            }

            public Param param;
            public SetModelPropertyOccluderCmd(string name, bool occluder) :
                base("setModelProperties")
            {
                this.param.name = name;
                this.param.occluder = occluder;
            }
        }

        [Serializable]
        protected class SetModelPropertyURICmd : CommandBase
        {
            [Serializable]
            public struct Param
            {
                public string name;
                public string uri;
            }

            public Param param;
            public SetModelPropertyURICmd(string name, string uri) :
                base("setModelProperties")
            {
                this.param.name = name;
                this.param.uri = uri;
            }
        }

        [Serializable]
        protected class SetGlobalObjectPoseCmd : CommandBase
        {
            private static readonly string defaultName = "setGlobalObjectPose";

            public InitPose param;
            public SetGlobalObjectPoseCmd(InitPose param) : base(defaultName)
            {
                this.param = param;
            }
        }

        [Serializable]
        private class Set1DRotationConstraintCommand : CommandBase
        {
            [Serializable]
            public class Parameters
            {
                public Vector3 up_world;
                public Vector3 up_model;
                public Vector3 center_model;
            }

            public Parameters param = new Parameters();

            public Set1DRotationConstraintCommand(
                Vector3 upWorld,
                Vector3 upModel,
                Vector3 centerModel) :
                base("set1DRotationConstraint")
            {
                this.param.up_world = upWorld;
                this.param.up_model = upModel;
                this.param.center_model = centerModel;
            }
        }

        [Serializable]
        protected class AddModelDataCmd : JsonAndBinaryCommandBase.DescriptionBase
        {
            public ModelDataDescriptorList param = new ModelDataDescriptorList();
            public AddModelDataCmd(ModelDataDescriptorList models) : base("addModelData")
            {
                this.param = models;
            }
        }
    }
}