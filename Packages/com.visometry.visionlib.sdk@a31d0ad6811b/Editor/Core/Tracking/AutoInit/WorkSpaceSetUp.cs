using System;
using UnityEditor;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @brief Adds menu entries to allow adding SimpleWorkSpace
     * scene.
     *  @ingroup WorkSpace
     */
    public static class WorkSpaceSetUp
    {
        private static void ConnectUsedCamera(WorkSpace workSpace)
        {
            var usedCamera = Camera.main;
            if (usedCamera == null)
            {
                LogHelper.LogWarning("Could not find Camera.", workSpace.gameObject);
            }
            else
            {
                workSpace.usedCamera = usedCamera;
            }
        }

        private static void CreateWorkSpaceManagerIfItDoesNotExist()
        {
            if (GameObject.FindObjectOfType<WorkSpaceManager>())
            {
                return;
            }
            var wsManagerGO = new GameObject("VLWorkSpaceManager");
            var wsManager = wsManagerGO.AddComponent<WorkSpaceManager>();
            wsManager.showProgressBar = true;
            wsManager.autoStartLearning = true;
            LogHelper.LogWarning(
                "Did not find a Workspace Manager in the scene, created a new one.", wsManagerGO);

            var worker = GameObject.FindObjectOfType<TrackingManager>();
            var parent = worker.gameObject.transform.parent;
            if (parent)
            {
                wsManagerGO.transform.parent = parent;
            }
        }

        private static void ConnectToScene(WorkSpace workSpace, GameObject destination)
        {
            // Ensure it gets re-parented if this was a context click (otherwise do nothing)
            if (destination && destination.transform.parent)
            {
                GameObjectUtility.SetParentAndAlign(
                    workSpace.gameObject, destination.transform.parent.gameObject);
            }
        }

        private static bool AddToTrackingAnchor(WorkSpace workSpace)
        {
            // Add to workSpaces Array of TrackingAnchor parent if one exists
            TrackingAnchor parentAnchor =
                workSpace.gameObject.GetComponentInParent<TrackingAnchor>();
            if (parentAnchor)
            {
                Array.Resize(ref parentAnchor.workSpaces, parentAnchor.workSpaces.Length + 1);
                parentAnchor.workSpaces[parentAnchor.workSpaces.Length - 1] = workSpace;
                EditorUtility.SetDirty(parentAnchor);
                LogHelper.LogWarning(
                    "Added the new WorkSpace to the parent TrackingAnchor.", parentAnchor);
            }
            return parentAnchor != null;
        }

        private static void AddGeometryChildren(
            AdvancedWorkSpace advancedWorkSpace,
            GameObject destination,
            float sourceSphereRadius)
        {
            WorkSpaceGeometry originGeometry =
                CreateGeometryObject("Origin", advancedWorkSpace.gameObject);
            originGeometry.sphere.radius = sourceSphereRadius;
            advancedWorkSpace.sourceObject = originGeometry;
            WorkSpaceGeometry destinationGeometry =
                CreateGeometryObject("Destination", advancedWorkSpace.gameObject);
            destinationGeometry.shape = WorkSpaceGeometry.Shape.Point;
            if (destination == null)
            {
                destination = destinationGeometry.gameObject;
            }
            advancedWorkSpace.destinationObject = destination;
        }

        private static WorkSpaceGeometry CreateGeometryObject(string newName, GameObject parent)
        {
            GameObject originGO = new GameObject(newName);
            GameObjectUtility.SetParentAndAlign(originGO, parent);
            return originGO.AddComponent<WorkSpaceGeometry>();
        }

        private static void Finalize(GameObject workSpaceGO)
        {
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(workSpaceGO, "Create " + workSpaceGO.name);
            Selection.activeObject = workSpaceGO;
        }

        [MenuItem("GameObject/VisionLib/AutoInit/Create Simple WorkSpace", false, 10)]
        private static void AddSimpleWorkSpace(MenuCommand menuCommand)
        {
            var destination = menuCommand.context as GameObject;

            var workSpaceGO = new GameObject("VLSimpleWorkSpace");
            var simpleWorkSpace = workSpaceGO.AddComponent<SimpleWorkSpace>();
            simpleWorkSpace.destinationObject = destination;
            ConnectUsedCamera(simpleWorkSpace);
            ConnectToScene(simpleWorkSpace, destination);
            AddToTrackingAnchor(simpleWorkSpace);
            CreateWorkSpaceManagerIfItDoesNotExist();
            Finalize(workSpaceGO);
        }

        [MenuItem("GameObject/VisionLib/AutoInit/Create Advanced WorkSpace", false, 10)]
        private static void AddAdvancedWorkSpace(MenuCommand menuCommand)
        {
            var destination = menuCommand.context as GameObject;

            var workSpaceGO = new GameObject("VLAdvancedWorkSpace");
            var advancedWorkSpace = workSpaceGO.AddComponent<AdvancedWorkSpace>();
            ConnectUsedCamera(advancedWorkSpace);
            ConnectToScene(advancedWorkSpace, destination);
            bool isMultiModelTracking = AddToTrackingAnchor(advancedWorkSpace);
            var radius = advancedWorkSpace.GetOptimalCameraDistance(destination);
            // Don't use destination in case of multi model tracking, since the tracking anchor
            // probably moves it.
            AddGeometryChildren(
                advancedWorkSpace, isMultiModelTracking ? null : destination, radius);
            CreateWorkSpaceManagerIfItDoesNotExist();
            Finalize(workSpaceGO);
        }
    }
}
