using UnityEngine;

namespace Visometry.VisionLib.SDK.Core.Details
{
    /// <summary>
    /// Utility functions for working with GameObjects and Components.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Returns the component of the given type if the GameObject has one attached, adds and
        /// returns it otherwise.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
            return gameObject.AddComponent<T>();
        }
    }
}