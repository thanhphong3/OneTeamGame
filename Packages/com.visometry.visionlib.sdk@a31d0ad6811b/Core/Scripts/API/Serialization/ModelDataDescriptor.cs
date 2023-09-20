using System.Collections.Generic;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// @ingroup API
    [System.Serializable]
    public struct BinaryDataDescriptor
    {
        public int binaryOffset;
        public int updateCount;
        public int vertexCount;
        public int triangleIndexCount;
        public int normalCount;
    }

    /// @ingroup API
    [System.Serializable]
    public class ModelDataDescriptor
    {
        public string name;
        public string type;
        public bool enabled;
        public bool occluder;
        public ModelTransform transform;
        public BinaryDataDescriptor[] subModels;
    }

    /// @ingroup API
    [System.Serializable]
    public class ModelDataDescriptorList
    {
        public List<ModelDataDescriptor> models = new List<ModelDataDescriptor>();
    }

    /// @ingroup API
    [System.Serializable]
    public class MeshList
    {
        public BinaryDataDescriptor[] meshes;
    }
}
