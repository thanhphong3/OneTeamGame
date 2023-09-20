using UnityEngine;
using System;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  This class contains shared properties and functionality of Advanced and Simple WorkSpaces.
    ///  **THIS IS SUBJECT TO CHANGE** Please do not rely on this code in productive environments.
    /// </summary>
    /// @ingroup WorkSpace
    [AddComponentMenu("VisionLib/Core/AutoInit/Simple WorkSpace")]
    public class SimpleWorkSpace : WorkSpace
    {
        [SerializeField]
        private SimpleSphereGeometry sourceGeometry = new SimpleSphereGeometry();

        public override BaseGeometry GetSourceGeometry()
        {
            return sourceGeometry;
        }

        protected override API.WorkSpace.Geometry GetDestinationGeometryDefinition()
        {
            return new API.WorkSpace.BoundingBox(
                BoundsUtilities.GetMeshBounds(this.destinationObject));
        }

        protected override API.WorkSpace.Geometry GetSourceGeometryDefinition()
        {
            return this.sourceGeometry.CreateGeometry(null);
        }

        public override API.WorkSpace.Definition GetWorkSpaceDefinition(bool useCameraRotation)
        {
            return base.GetWorkSpaceDefinitionFromType(
                API.WorkSpace.Definition.Type.Simple, useCameraRotation);
        }

        public override Vector3 GetCenter()
        {
            return WorkSpace.GetCenter(this.destinationObject);
        }

        public override float GetSize()
        {
            // radius of simple sphere geometry
            return GetSimpleSphereRadius() * 1.2f;
        }

        public float GetSimpleSphereRadius()
        {
            return base.GetOptimalCameraDistance(this.destinationObject);
        }

        public override int GetVerticesCount()
        {
            return this.sourceGeometry.currentMesh.Length * GetDestinationVertices().Length;
        }

        protected override Transform GetRootTransform()
        {
            var trackingModel = this.destinationObject.GetComponent<TrackingModel>();
            if (trackingModel != null)
            {
                return trackingModel.rootTransform;
            }
            return null;
        }
    }
}
