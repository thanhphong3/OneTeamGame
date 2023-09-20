using UnityEngine;
using System;
using UnityEngine.Serialization;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    ///  This class contains shared properties and functionality of Advanced and Simple WorkSpaces.
    ///  **THIS IS SUBJECT TO CHANGE** Please do not rely on this code in productive environments.
    /// </summary>
    /// @ingroup WorkSpace
    [AddComponentMenu("VisionLib/Core/AutoInit/WorkSpace")]
    public abstract class WorkSpace : MonoBehaviour
    {
        [Tooltip(
            "Camera that is used to preview the AutoInit poses. " +
            "Attention: Transform of this camera will be changed by this feature!")]
        public Camera usedCamera;

        [FormerlySerializedAs("destinationGeometry")]
        [Tooltip(
            "Use any GameObject from the scene to set the destination to its center or use the destination child of WorkSpace")]
        public GameObject destinationObject;

        [Tooltip("Display dotted lines between all origin and destination points")]
        public bool displayViewDirection = true;

        [SerializeField]
        [Tooltip("The up-Vector of your 3D object")]
        protected Vector3 upVector = Vector3.up;

        [SerializeField]
        private int camSliderPosition;

        protected const float defaultRotationRange = 20.0f;
        protected const float defaultRotationStep = 20.0f;
        private const float defaultFieldOfView = 60.0f;

        private void Awake()
        {
            if (this.usedCamera == null)
            {
                this.usedCamera = Camera.main;
            }
        }

        /// <summary>
        /// Gets the local positions related to the destination object.
        /// If the destinationObject is a Renderer, it will calculate the center of the model.
        /// </summary>
        /// <returns>Array of local points that represent the object geometry</returns>
        public Vector3[] GetDestinationVertices()
        {
            if (!this.destinationObject)
            {
                return new Vector3[] {};
            }

            // check if object has a geometry component
            WorkSpaceGeometry vlDestinationGeometry =
                this.destinationObject.GetComponent<WorkSpaceGeometry>();
            if (vlDestinationGeometry != null)
            {
                vlDestinationGeometry.GetGeometry().UpdateMesh();
                return vlDestinationGeometry.GetGeometry().currentMesh;
            }

            // if not, we use the renderer
            return new[] { GetCenter(this.destinationObject) };
        }

        private float GetFieldOfView()
        {
            if (!this.usedCamera)
            {
                return defaultFieldOfView;
            }

            float verticalFoV = this.usedCamera.fieldOfView;
            float horizontalFoV =
                Camera.VerticalToHorizontalFieldOfView(verticalFoV, this.usedCamera.aspect);
            float usedFieldOfView = Mathf.Min(horizontalFoV, verticalFoV);
            return (usedFieldOfView > 0) ? usedFieldOfView : defaultFieldOfView;
        }

        private float RoundToNearestMultiple(float number, float factor)
        {
            return (float) Math.Round(number / factor) * factor;
        }

        private float GetApproximateFieldOfView()
        {
            const float factor = 0.5f;
            float roundedFov = RoundToNearestMultiple(GetFieldOfView(), factor);
            return Math.Max(roundedFov, factor);
        }

        protected API.WorkSpace.Definition GetWorkSpaceDefinitionFromType(
            API.WorkSpace.Definition.Type type,
            bool useCameraRotation)
        {
            var vlUpVector = CameraHelper.UnityVectorToVLVector(this.upVector);
            if (vlUpVector == Vector3.zero)
            {
                vlUpVector = Vector3.up;
            }

            API.ModelTransform modelTransform = new API.ModelTransform(this.gameObject.transform, GetRootTransform());
            var workspaceTrafo = new API.WorkSpace.Transform(modelTransform.t, modelTransform.q);
            var currentWorkSpaceDef = new API.WorkSpace.Definition(
                workspaceTrafo,
                vlUpVector,
                useCameraRotation ? defaultRotationRange : 0.0f,
                defaultRotationStep,
                GetApproximateFieldOfView(),
                type);

            currentWorkSpaceDef.origin = GetSourceGeometryDefinition();
            currentWorkSpaceDef.destination = GetDestinationGeometryDefinition();

            return currentWorkSpaceDef;
        }

        public API.WorkSpace.Transform[] GetCameraTransforms()
        {
            bool useCameraRotation = false;
            return this.GetWorkSpaceDefinition(useCameraRotation).GetCameraTransforms();
        }

        public static Vector3 GetCenter(GameObject go)
        {
            return BoundsUtilities.GetMeshBounds(go).center;
        }

        public float GetOptimalCameraDistance(GameObject destinationForBounds)
        {
            float boundingBoxDiagonal =
                BoundsUtilities.GetMeshBounds(destinationForBounds).size.magnitude;
            return boundingBoxDiagonal * 0.5f /
                   Mathf.Sin(GetApproximateFieldOfView() * 0.5f / 180f * Mathf.PI);
        }

        protected static API.WorkSpace.Transform
            CreateVLTransformFromObject(GameObject sourceObject)
        {
            return CameraHelper.CreateVLTransform(sourceObject, false);
        }

        public abstract BaseGeometry GetSourceGeometry();

        protected abstract API.WorkSpace.Geometry GetDestinationGeometryDefinition();

        protected abstract API.WorkSpace.Geometry GetSourceGeometryDefinition();

        protected abstract Transform GetRootTransform();

        /// <summary>
        /// Creates a WorkSpace.Definition from this WorkSpace.
        /// </summary>
        /// <returns>WorkSpace.Definition described by this class</returns>
        public abstract API.WorkSpace.Definition GetWorkSpaceDefinition(bool useCameraRotation);

        public abstract Vector3 GetCenter();

        /// <summary>
        /// Calculates the WorkSpace boundaries
        /// using the origin and destination bounds
        /// and the distance between them.
        /// </summary>
        /// <returns>WorkSpace size</returns>
        public abstract float GetSize();

        public abstract int GetVerticesCount();
    }
}
