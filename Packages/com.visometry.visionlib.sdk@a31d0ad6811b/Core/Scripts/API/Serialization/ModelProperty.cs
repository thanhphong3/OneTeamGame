using System;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// <summary>
    ///  ModelProperties stores the internal states of the model which is used for model based
    ///  tracking.
    /// Such an object is usually passed, when subscribing to the OnGetModelProperties event.
    /// </summary>
    /// @ingroup API
    [Serializable]
    public class ModelProperties
    {
        /// <summary>
        /// The internal id or name used for addressing the model (e.g. SetModelProperty)
        /// A name for setting the property should always carry an uri prefix scheme: 'name:'
        /// </summary>
        public string name;

        /// <summary>
        /// The uri that has been used to load the model.
        /// </summary>
        public string uri;

        /// <summary>
        /// States if the model is currently enabled.
        /// </summary>
        public bool enabled;

        /// <summary>
        /// States if the model is used for occlusion.
        /// </summary>
        public bool occluder;

        public ModelProperties(string name, string uri, bool enabled, bool occluder)
        {
            this.name = name;
            this.uri = uri;
            this.enabled = enabled;
            this.occluder = occluder;
        }
    }

    /// <summary>
    ///  ModelProperties stores the internal states of the model which is used for model based
    ///  tracking.
    /// Such an object is usually passed, when subscribing to the OnGetModelProperties event.
    /// </summary>
    /// @ingroup API
    [Serializable]
    public class ModelPropertiesStructure
    {
        public ModelProperties[] info;
    }
}