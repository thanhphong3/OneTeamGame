using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  Behaviour, which enables the user to use the GameObjects mesh for tracking. NOTE: This
    ///  behaviour is considered as BETA. Please do not alter this file unless you know what you are
    ///  doing - it is considered to be changed and optimized in future versions.
    ///
    /// USAGE: You can add this behaviour to an UnityObject which might contain a mesh. You can
    /// allow the transmission of the model inside your definition using the useForTracking flag. If
    /// you enable the occluder property, it will be rendered for occlusion of objects while
    /// tracking. If you want to disable the visibility of the object in Unity, disable the mesh.
    ///
    /// </summary>
    /// @ingroup Core
    [System.Serializable]
    [AddComponentMenu("VisionLib/Core/Tracking Model")]
    public class TrackingModel : TrackingManagerReference
    {
        private struct ModelData
        {
            public Mesh mesh;
            public Matrix4x4 transform;
        }

        /// <summary>
        /// Gives the amount of data, which will be encoded into the ModelDataDescriptor.
        /// </summary>
        private enum DetailLevel
        {
            Complete, // All data (parameters and description of model data
            Parameters // Only parameters of the model (transformation, model (de) activation, etc.)
        }

        private bool internalUseForTracking = true;
        private bool internalOccluder = false;
#if !UNITY_WSA_10_0
        private bool modelLoaded = false;
#endif

        private TransformCache transformInBackend;

        // React to changes on these variables

        /// <summary>
        ///  ModelTracker used in this tracking scenario.
        ///  This behaviour tries to find the corresponding ModelTracker automatically if not
        ///  defined.
        /// </summary>
        [FormerlySerializedAs("modelTrackerBehaviour")]
        protected ModelTracker modelTracker;

        /// <summary>
        /// Only needs to be set in ARFoundation or HoloLens scenes.
        /// "Root" node of the object that is moved by the tracker.
        /// On HoloLens, this is relevant for calculating the correct
        /// (relative) transform for models.
        /// </summary>
        [Tooltip(
            "In ARFoundation or HoloLens scenarios, set this to the root GameObject that is moved by the tracker, e.g. `SceneContent`.")]
        public Transform rootTransform;

        /// <summary>
        ///  Enable/Disable this property in order to use/not use all models in this object for
        ///  tracking.
        /// </summary>
        public bool useForTracking = true;

        /// <summary>
        ///  Enable/Disable this property in order to use/not use all models in this object for
        ///  occlusion tracking.
        /// </summary>
        public bool occluder = false;

        /// <summary>
        ///  List of model descriptions, which are currently used for tracking.
        /// </summary>
        public List<ModelDeserializationResult> modelDescriptions;

        /// <summary>
        /// Update count, which will be propagated to the tracking system.
        /// </summary>
        private int globalUpdateCount = 0;
        /// <summary>
        /// Temporary variable, which counts, how many bytes are used for the
        /// binary data.
        /// </summary>
        private int binaryOffset = 0;
        /// <summary>
        /// Temporary list of all meshes, which will be added to the binary data.
        /// </summary>
        private List<ModelData> modelData = new List<ModelData>();

        /// <summary>
        ///  Enables the model in the tracking system. If occluder is true, it will
        ///  be used as an occlusion geometry.
        /// </summary>
        public void SetUseModelForTracking(bool enable)
        {
            if (enable == this.internalUseForTracking)
            {
                return;
            }

            this.useForTracking = enable;
            this.internalUseForTracking = enable;

            foreach (ModelDeserializationResult description in modelDescriptions)
            {
                this.modelTracker.SetModelPropertyEnabled(description.name, enable);
            }
        }

        /// <summary>
        ///  Defines, if this mesh should be used as an occlusion geometry in the
        ///  tracking system. It is only active, if useForTracking is true.
        /// </summary>
        public void SetUseAsOccluder(bool enable)
        {
            if (enable == this.internalOccluder)
            {
                return;
            }

            this.occluder = enable;
            this.internalOccluder = enable;

            foreach (ModelDeserializationResult description in modelDescriptions)
            {
                this.modelTracker.SetModelPropertyOccluder(description.name, enable);
            }
        }

        /// <summary>
        ///  Add all (sub)meshes to the tracking system. If a sub mesh has its own
        ///  TrackingModel, it will not be added, but this behaviour should
        ///  manage the relevant submeshes.
        /// </summary>
        public async Task AddModelDataAsync()
        {
            var modelDescriptors = this.GenerateModelDataDescriptors(DetailLevel.Complete);
            byte[] binaryData = this.GenerateBinaryData(this.modelData, this.binaryOffset);

            GCHandle binaryDataHandle = GCHandle.Alloc(binaryData, GCHandleType.Pinned);
            IntPtr data = binaryDataHandle.AddrOfPinnedObject();
            UInt32 dataLength = Convert.ToUInt32(binaryData.Length);

            try
            {
                var deserializationResults = await ModelTrackerCommands.AddModelDataAsync(
                    this.worker, modelDescriptors, data, dataLength);

                if (deserializationResults != null)
                {
                    this.modelDescriptions =
                        new List<ModelDeserializationResult>(deserializationResults.addedModels);
                }
#if !UNITY_WSA_10_0
                this.modelLoaded = true;
#endif
            }
            finally
            {
                // free data previously allocated/pinned
                binaryDataHandle.Free();
            }
        }

        /// <summary>
        ///  Add all (sub)meshes to the tracking system. If a sub mesh has its own
        ///  TrackingModel, it will not be added, but this behaviour should
        ///  manage the relevant submeshes.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        public void AddModelData()
        {
            TrackingManager.CatchCommandErrors(AddModelDataAsync(), this);
        }

        /// <summary>
        ///  Updates the transformation of all (sub)meshes in the tracking system.
        ///  It has to be called after each update in a transform which is relevant
        ///  for the location of a related mesh.
        /// </summary>
        /// <param name="useAllChildNodes">
        ///  If useAllChildNodes is true, this will update all locations of
        ///  submeshes, even if they have their own TrackingModel. It does
        ///  not update the modelDescriptions of this behaviour.
        /// </param>
        public async Task UpdateModelPropertiesAsync(bool useAllChildNodes)
        {
            var modelDescriptors =
                this.GenerateModelDataDescriptors(DetailLevel.Parameters, useAllChildNodes);

            await ModelTrackerCommands.SetMultipleModelPropertiesAsync(
                this.worker, modelDescriptors);
        }

        /// <summary>
        ///  Updates the transformation of all (sub)meshes in the tracking system.
        ///  It has to be called after each update in a transform which is relevant
        ///  for the location of a related mesh.
        /// </summary>
        /// <remarks> This function will be performed asynchronously.</remarks>
        /// <param name="useAllChildNodes">
        ///  If useAllChildNodes is true, this will update all locations of
        ///  submeshes, even if they have their own TrackingModel. It does
        ///  not update the modelDescriptions of this behaviour.
        /// </param>
        public void UpdateModelProperties(bool useAllChildNodes)
        {
            TrackingManager.CatchCommandErrors(UpdateModelPropertiesAsync(useAllChildNodes), this);
        }

        /// <summary>
        /// Generates a list of ModelDataDescriptor from all meshes, which
        /// will be administered by this behaviour.
        /// </summary>
        /// <param name="detailLevel"></param>
        /// Gives the amount of data, which will be encoded into the ModelDataDescriptor
        /// <param name="useAllChildNodes">
        /// If true: Also process child nodes, which are administered by another
        /// TrackingModel.
        /// </param>
        /// <returns>List of all ModelDataDescriptor from all meshes, which
        /// will be administered by this behaviour.</returns>
        private ModelDataDescriptorList
            GenerateModelDataDescriptors(DetailLevel detailLevel, bool useAllChildNodes = false)
        {
            // Reset global data
            this.binaryOffset = 0;
            this.modelData.Clear();
            this.globalUpdateCount++;

            var modelDataDescriptors = new ModelDataDescriptorList();
            GenerateModelDataDescriptorsRecursively(
                this.transform, detailLevel, useAllChildNodes, ref modelDataDescriptors.models);
            return modelDataDescriptors;
        }

        /// <summary>
        /// Iterates through all Child nodes and creates ModelDescriptions of these nodes, which
        /// will added to the modelDataDescriptors.
        /// </summary>
        /// <param name="transform">
        /// Transform, which is searched for possible meshes.
        /// </param>
        /// <param name="detailLevel">
        /// Gives the amount of data, which will be encoded into the ModelDataDescriptor
        /// </param>
        /// <param name="useAllChildNodes">
        /// If true: Also process child nodes, which are administered by another
        /// TrackingModel
        /// </param>
        /// <param name="modelDataDescriptors">
        /// List of Descriptions of the mesh inside the transform and in all child nodes.
        /// </param>
        private void GenerateModelDataDescriptorsRecursively(
            Transform transform,
            DetailLevel detailLevel,
            bool useAllChildNodes,
            ref List<ModelDataDescriptor> modelDataDescriptors)
        {
            MeshFilter mesh = transform.GetComponent<MeshFilter>();

            // If child node has a mesh, add this to the list
            if (mesh)
            {
                ModelDataDescriptor descriptor =
                    CreateModelDataDescriptorForTransform(transform, detailLevel, useAllChildNodes);
                // If node is inactive or should not be added to commandDescriptor
                // do not iterate through the child nodes.
                if (descriptor == null)
                {
                    return;
                }
                modelDataDescriptors.Add(descriptor);
            }

            // Add child nodes
            foreach (Transform child in transform)
            {
                GenerateModelDataDescriptorsRecursively(
                    child, detailLevel, useAllChildNodes, ref modelDataDescriptors);
            }
        }

        /// <summary>
        /// Creates a ModelDataDescriptor of the mesh inside the transform
        /// </summary>
        /// <param name="transform">
        /// Transform, which is searched for possible meshes.
        /// </param>
        /// <param name="detailLevel">
        /// Gives the amount of data, which will be encoded into the ModelDataDescriptor
        /// </param>
        /// <param name="useAllChildNodes">
        /// If true: Also process child nodes, which are administered by another
        /// TrackingModel
        /// </param>
        /// <returns>Description of the mesh inside the transform.</returns>
        private ModelDataDescriptor CreateModelDataDescriptorForTransform(
            Transform transform,
            DetailLevel detailLevel,
            bool useAllChildNodes)
        {
            // If transform is not active, do not add the model
            if (!transform.gameObject.activeInHierarchy)
            {
                return null;
            }

            // See if another TrackingModel is active in this transform. If
            // this is the case, break execution of this node and its children.
            TrackingModel trackable = transform.GetComponent<TrackingModel>();
            if (!useAllChildNodes && trackable && trackable != this && trackable.enabled)
            {
                return null;
            }
            if (trackable == null)
            {
                trackable = this;
            }

            MeshFilter mesh = transform.GetComponent<MeshFilter>();
            string uniqueUnityModelID = mesh.GetInstanceID().ToString();

            ModelDataDescriptor descriptor = new ModelDataDescriptor();
            descriptor.name = uniqueUnityModelID;
            descriptor.type = "model";
            descriptor.enabled = trackable.useForTracking;
            descriptor.occluder = trackable.occluder;
            descriptor.transform = new ModelTransform(transform, this.rootTransform);
            if (detailLevel == DetailLevel.Complete)
            {
                descriptor.subModels = new BinaryDataDescriptor[] { CreateDataDescriptor(mesh) };
            }

            return descriptor;
        }

        /// <summary>
        /// Creates a description of the mesh data of this meshfilter. Internally
        /// updates the binaryOffset and adds the ModelData to the modelData list.
        /// </summary>
        /// <param name="filter">
        /// MeshFilter of the mesh, which will be processed.
        /// </param>
        /// <returns></returns>
        private BinaryDataDescriptor CreateDataDescriptor(MeshFilter filter)
        {
            Mesh mesh = filter.mesh;

            BinaryDataDescriptor descriptor = new BinaryDataDescriptor();
            descriptor.binaryOffset = this.binaryOffset;
            descriptor.updateCount = this.globalUpdateCount;
            descriptor.vertexCount = mesh.vertices.Length;
            descriptor.triangleIndexCount = mesh.triangles.Length;
            descriptor.normalCount = mesh.normals.Length;

            // Vertices
            this.binaryOffset += mesh.vertices.Length * 3 * sizeof(float);
            // Triangles
            this.binaryOffset += mesh.triangles.Length * sizeof(UInt32);
            // Normals
            this.binaryOffset += mesh.normals.Length * 3 * sizeof(float);

            ModelData data = new ModelData();
            data.mesh = mesh;
            data.transform = filter.transform.worldToLocalMatrix;

            this.modelData.Add(data);

            return descriptor;
        }

        /// <summary>
        /// Transforms the ModelData list to a byte array of length dataSize
        /// </summary>
        /// <param name="modelData">
        /// List of all the models, which should be added to the byte array.
        /// </param>
        /// <param name="dataSize">
        /// Length of the generated byte array. This has to be the size which is
        /// required for all the meshes given in modelData.
        /// </param>
        /// <returns></returns>
        private byte[] GenerateBinaryData(List<ModelData> modelData, int dataSize)
        {
            // Generate Data Buffer
            byte[] binaryData = new byte[dataSize];
            int binaryDataOffset = 0;
            foreach (ModelData data in modelData)
            {
                SerializeModel(data, ref binaryData, ref binaryDataOffset);
            }
            return binaryData;
        }

        /// <summary>
        /// Adds the data of the given model to the byte array. The internal binary
        /// structure of each model is
        ///  - vertices (3 floats per vertex; vertexCount vertices)
        ///  - indices (triangleIndexCount UInt32)
        ///  - normals (3 floats per normal; normalCount normals)
        /// </summary>
        /// <param name="data">
        /// ModelData of the model, which should be added to the byte array.
        /// </param>
        /// <param name="binaryData">
        /// Target byte array, in which the model will be serialized.
        /// </param>
        /// <param name="binaryDataOffset">
        /// Current index, where data can be written in the array without
        /// overriding previously added data.
        /// </param>
        private void SerializeModel(ModelData data, ref byte[] binaryData, ref int binaryDataOffset)
        {
            Mesh mesh = data.mesh;

            /* Binary Structure
             * - vertexCount * 3 float: vertices
             * - triangleIndexCount UInt32: indices
             * - normalCount * float: normals
             */

            // Vertices
            foreach (Vector3 vertex in mesh.vertices)
            {
                // The flip of the x-axis is necessary for streaming the model data
                // into the vlSDK.
                float[] vector = { -vertex.x, vertex.y, vertex.z };

                Buffer.BlockCopy(vector, 0, binaryData, binaryDataOffset, 3 * sizeof(float));
                binaryDataOffset += 3 * sizeof(float);
            }

            // Triangles
            Buffer.BlockCopy(
                mesh.triangles,
                0,
                binaryData,
                binaryDataOffset,
                mesh.triangles.Length * sizeof(UInt32));
            binaryDataOffset += mesh.triangles.Length * sizeof(UInt32);

            // Normals
            foreach (Vector3 normal in mesh.normals)
            {
                float[] vector = { -normal.x, normal.y, normal.z };

                Buffer.BlockCopy(vector, 0, binaryData, binaryDataOffset, 3 * sizeof(float));
                binaryDataOffset += 3 * sizeof(float);
            }
        }

        private void FindModelTracker()
        {
            // ModelTracker specified explicitly?
            if (this.modelTracker != null)
            {
                return;
            }

            // Look for it at the same GameObject first
            this.modelTracker = GetComponent<ModelTracker>();
            if (this.modelTracker != null)
            {
                return;
            }

            // Try to find it anywhere in the scene
            this.modelTracker = FindObjectOfType<ModelTracker>();
            if (this.modelTracker != null)
            {
                return;
            }
            LogHelper.LogError(
                "No ModelTracker found. Please add a ModelTracker somewhere in your scene.");
        }

        private void InitializeTransformCache()
        {
            if (this.transformInBackend != null)
            {
                return;
            }
            this.transformInBackend = (new TransformCache(delegate() {
                UpdateModelProperties(true);
            })); // Additional parentheses to work around clang-format bug
        }

        private void Start()
        {
            InitializeTransformCache();
            FindModelTracker();
        }

        private void Update()
        {
            SetUseModelForTracking(this.useForTracking);
            SetUseAsOccluder(this.occluder);
            // Hack to fix HoloLensDynamicModelTracking scene, see vlUnitySDK#795
#if !UNITY_WSA_10_0
            if (this.modelLoaded)
            {
                this.transformInBackend.Write(this.transform);
            }
#endif
        }

        private void OnEnable()
        {
            InitializeTransformCache();
            FindModelTracker();

            TrackingManager.OnTrackerInitialized += AddModelData;

            if (this.trackingManager.GetTrackerInitialized())
            {
                this.AddModelData();
            }
        }

        private void OnDisable()
        {
            try
            {
                if (this.trackingManager.GetTrackerInitialized())
                {
#if !UNITY_WSA_10_0
                    this.modelLoaded = false;
#endif
                    foreach (ModelDeserializationResult description in this.modelDescriptions)
                    {
                        this.modelTracker.RemoveModel(description.name);
                    }
                    this.transformInBackend.Invalidate();
                }
            }
            catch (TrackingManagerNotFoundException)
            {
            }
            this.modelDescriptions.Clear();
            TrackingManager.OnTrackerInitialized -= AddModelData;
        }
    }
}
