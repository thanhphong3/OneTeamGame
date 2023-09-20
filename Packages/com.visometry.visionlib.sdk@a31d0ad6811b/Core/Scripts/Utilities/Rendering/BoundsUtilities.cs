using System;
using UnityEngine;

namespace Visometry.VisionLib.SDK.Core
{
    public static class BoundsUtilities
    {
        public static Bounds GetMeshBounds(GameObject parent)
        {
            if (parent == null)
            {
                return new Bounds();
            }

            var meshFilters = parent.transform.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0)
            {
                return new Bounds(parent.transform.position, Vector3.zero);
            }

            var bounds = meshFilters[0].sharedMesh.bounds;
            foreach (var meshFilter in meshFilters)
            {
                bounds.Encapsulate(meshFilter.sharedMesh.bounds);
            }
            return bounds;
        }
    }
}
