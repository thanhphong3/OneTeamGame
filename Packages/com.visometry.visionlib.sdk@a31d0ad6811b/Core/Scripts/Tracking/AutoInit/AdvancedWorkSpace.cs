﻿using UnityEngine;
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
    [AddComponentMenu("VisionLib/Core/AutoInit/Advanced WorkSpace")]
    public class AdvancedWorkSpace : WorkSpace
    {
        [FormerlySerializedAs("sourceGeometry")]
        [Tooltip("The origin geometry (child of the WorkSpace).")]
        public WorkSpaceGeometry sourceObject;

        /// <summary>
        /// Only needs to be set in ARFoundation or HoloLens scenes.
        /// "Root" node of the object that is moved by the tracker.
        /// On HoloLens, this is relevant for calculating the correct
        /// (relative) transform for models.
        /// </summary>
        [Tooltip(
            "In ARFoundation or HoloLens scenarios, set this to the root GameObject that is moved by the tracker, e.g. `SceneContent`.")]
        public Transform rootTransform;

        public override BaseGeometry GetSourceGeometry()
        {
            return sourceObject ? sourceObject.GetGeometry() : null;
        }

        protected override API.WorkSpace.Geometry GetDestinationGeometryDefinition()
        {
            WorkSpaceGeometry destinationGeometry =
                this.destinationObject.GetComponent<WorkSpaceGeometry>();

            // check if object has a geometry component
            if (destinationGeometry != null)
            {
                return destinationGeometry.GetGeometry().CreateGeometry(
                    WorkSpace.CreateVLTransformFromObject(this.destinationObject));
            }
            else
            {
                // if target object has no geometry component -> use center point which is stored in
                // destinationPoints[0]
                // apply parent transform of destination object to the local position of the center
                Vector3 destinationPositionInWorld =
                    this.destinationObject.transform.TransformPoint(GetDestinationVertices()[0]);
                Vector3 destinationPositionInWorkSpace =
                    this.transform.InverseTransformPoint(destinationPositionInWorld);

                Quaternion q = Quaternion.identity;
                CameraHelper.ToVLInPlace(ref destinationPositionInWorkSpace, ref q);
                return new API.WorkSpace.Plane(
                    0,
                    0,
                    1,
                    new API.WorkSpace.Transform(
                        destinationPositionInWorkSpace, Quaternion.identity));
            }
        }

        protected override API.WorkSpace.Geometry GetSourceGeometryDefinition()
        {
            return this.sourceObject.GetGeometry().CreateGeometry(
                WorkSpace.CreateVLTransformFromObject(this.sourceObject.gameObject));
        }

        public override API.WorkSpace.Definition GetWorkSpaceDefinition(bool useCameraRotation)
        {
            return base.GetWorkSpaceDefinitionFromType(
                API.WorkSpace.Definition.Type.Advanced, useCameraRotation);
        }

        public override Vector3 GetCenter()
        {
            if (this.sourceObject == null && this.destinationObject == null)
            {
                return Vector3.zero;
            }

            if (this.sourceObject == null)
            {
                return this.destinationObject.transform.position;
            }

            if (this.destinationObject == null)
            {
                return this.sourceObject.transform.position;
            }

            return (this.sourceObject.transform.position +
                    this.destinationObject.transform.position) /
                   2f;
        }

        public override float GetSize()
        {
            if (this.sourceObject == null)
            {
                return 1f;
            }

            float sizeOfSource = this.sourceObject.GetGeometry().GetGeometrySize();
            float sizeOfDestination = 0f;

            if (this.destinationObject == null)
            {
                return sizeOfSource;
            }

            if (this.destinationObject.GetComponent<WorkSpaceGeometry>())
            {
                sizeOfDestination = this.destinationObject.GetComponent<WorkSpaceGeometry>()
                                        .GetGeometry()
                                        .GetGeometrySize();
            }

            float distance = Vector3.Distance(
                this.sourceObject.transform.position, this.destinationObject.transform.position);

            return Math.Max(sizeOfSource, sizeOfDestination) + distance / 2f;
        }

        public override int GetVerticesCount()
        {
            if (this.sourceObject == null || this.destinationObject == null)
            {
                return 0;
            }

            return this.sourceObject.GetGeometry().currentMesh.Length *
                   GetDestinationVertices().Length;
        }

        protected override Transform GetRootTransform()
        {
            var trackingModel = this.destinationObject.GetComponent<TrackingModel>();
            if (trackingModel != null)
            {
                this.rootTransform = trackingModel.rootTransform;
            }
            return this.rootTransform;
        }
    }
}