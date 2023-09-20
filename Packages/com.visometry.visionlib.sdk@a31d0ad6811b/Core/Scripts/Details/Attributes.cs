using System;
using UnityEngine;

namespace Visometry.VisionLib.SDK.Core.Details
{
    /// <summary>
    /// Add the [DisplayName("newName")] Attribute above a public parameter
    /// to make its Inspector label show 'newName' instead of the parameter name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DisplayNameAttribute : PropertyAttribute
    {
        public readonly string displayName;
        public DisplayNameAttribute(string displayName)
        {
            this.displayName = displayName;
        }
    }

    /// <summary>
    /// Add the [OnlyShowIf("fieldToCheck")] Attribute above a public parameter
    /// to only show the parameter in the Inspector, if the bool value of "fieldToCheck" is
    /// true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class OnlyShowIfAttribute : PropertyAttribute
    {
        public readonly string fieldToCheck;
        public readonly bool showOnValue;
        public OnlyShowIfAttribute(string fieldToCheck, bool showOnValue)
        {
            this.fieldToCheck = fieldToCheck;
            this.showOnValue = showOnValue;
        }
    }

    /// <summary>
    /// Add the [FilePathReferenceField("MyLabel", ".myExtension", false)] Attribute
    /// above a public FilePathReference to draw its custom appearance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FilePathReferenceFieldAttribute : PropertyAttribute
    {
        public enum AllowProjectDir { Yes, No }
        public enum Mandatory { Yes, No }

        public readonly string displayLabel;
        public readonly string fileEnding;
        public readonly bool mandatory;
        public readonly bool allowProjectDir;

        public FilePathReferenceFieldAttribute(
            string displayLabel,
            string fileEnding,
            Mandatory mandatory,
            AllowProjectDir allowProjectDir)
        {
            this.displayLabel = displayLabel;
            this.fileEnding = fileEnding;
            this.mandatory = (mandatory == Mandatory.Yes);
            this.allowProjectDir = (allowProjectDir == AllowProjectDir.Yes);
        }
    }

}
