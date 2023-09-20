using UnityEngine;
using Visometry.VisionLib.SDK.Core.API;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core.Details
{
    /// <summary>
    ///  Static class with helper functions and constants for doing camera
    ///  transformations.
    /// </summary>
    public static class CameraHelper
    {
        /// <summary>
        ///  Transformation matrix with a 0 degree rotation around the z-axis
        ///  (identity matrix).
        /// </summary>
        public static readonly Matrix4x4 rotationZ0 = Matrix4x4.identity;
        /// <summary>
        ///  Transformation matrix with a 90 degree rotation around the z-axis.
        /// </summary>
        public static readonly Matrix4x4 rotationZ90 =
            Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90.0f, Vector3.forward), Vector3.one);
        /// <summary>
        ///  Transformation matrix with a 180 degree rotation around the z-axis.
        /// </summary>
        public static readonly Matrix4x4 rotationZ180 =
            Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180.0f, Vector3.forward), Vector3.one);
        /// <summary>
        ///  Transformation matrix with a 270 degree rotation around the z-axis.
        /// </summary>
        public static readonly Matrix4x4 rotationZ270 =
            Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(270.0f, Vector3.forward), Vector3.one);

        /// <summary>
        ///  Transformation matrix with a 180 degree rotation around the y-axis.
        /// </summary>
        public static readonly Matrix4x4 rotationY180 =
            Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180.0f, Vector3.up), Vector3.one);

        public static readonly Matrix4x4 flipZ = Matrix4x4.Scale(new Vector3(1, 1, -1));

        public static readonly Matrix4x4 flipX = Matrix4x4.Scale(new Vector3(-1, 1, 1));

        public static readonly Matrix4x4 flipXY = Matrix4x4.Scale(new Vector3(-1, -1, 1));

        public static readonly Matrix4x4 flipYZ = Matrix4x4.Scale(new Vector3(1, -1, -1));

        public static readonly Matrix4x4 flipXYZ = Matrix4x4.Scale(new Vector3(-1, -1, -1));

        /// <summary>
        ///  Extracts the rotation from a 4x4 transformation matrix as Quaternion.
        /// </summary>
        /// <returns>
        ///  Quaternion with rotation extracted from given matrix.
        /// </returns>
        /// <param name="m">
        ///  4x4 transformation matrix.
        /// </param>
        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            // Source:
            // http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.html
            Quaternion q = new Quaternion();
            float trace = m[0, 0] + m[1, 1] + m[2, 2];
            if (trace > 0.0f)
            {
                float s = 0.5f / Mathf.Sqrt(trace + 1.0f);
                q.w = 0.25f / s;
                q.x = (m[2, 1] - m[1, 2]) * s;
                q.y = (m[0, 2] - m[2, 0]) * s;
                q.z = (m[1, 0] - m[0, 1]) * s;
            }
            else
            {
                if (m[0, 0] > m[1, 1] && m[0, 0] > m[2, 2])
                {
                    float s = 2.0f * Mathf.Sqrt(1.0f + m[0, 0] - m[1, 1] - m[2, 2]);
                    q.w = (m[2, 1] - m[1, 2]) / s;
                    q.x = 0.25f * s;
                    q.y = (m[0, 1] + m[1, 0]) / s;
                    q.z = (m[0, 2] + m[2, 0]) / s;
                }
                else if (m[1, 1] > m[2, 2])
                {
                    float s = 2.0f * Mathf.Sqrt(1.0f + m[1, 1] - m[0, 0] - m[2, 2]);
                    q.w = (m[0, 2] - m[2, 0]) / s;
                    q.x = (m[0, 1] + m[1, 0]) / s;
                    q.y = 0.25f * s;
                    q.z = (m[1, 2] + m[2, 1]) / s;
                }
                else
                {
                    float s = 2.0f * Mathf.Sqrt(1.0f + m[2, 2] - m[0, 0] - m[1, 1]);
                    q.w = (m[1, 0] - m[0, 1]) / s;
                    q.x = (m[0, 2] + m[2, 0]) / s;
                    q.y = (m[1, 2] + m[2, 1]) / s;
                    q.z = 0.25f * s;
                }
            }
            return q;
        }

        /// <summary>
        ///  Computes the pose in VisionLib coordinates from a Unity Camera object.
        /// </summary>
        /// <param name="camera">
        ///  Camera object which should be used for computing the pose.
        /// </param>
        /// <param name="offset">
        ///  Transformation which will be applied to the transformation of the
        ///  camera before computing the pose. This is useful in case the screen
        ///  orientation was changed. Unity will then automatically rotate the
        ///  scene, but the camera image will not be rotated. Therefore you need to
        ///  reverse the automatic rotation from Unity. You might want to use the
        ///  rotationZ0, rotationZ90, rotationZ180 and rotationZ270 constants as
        ///  values for this parameter.
        /// </param>
        /// <param name="t">
        ///  Translation in VisionLib coordinates.
        /// </param>
        /// <param name="q">
        ///  Rotation in VisionLib coordinates.
        /// </param>
        public static void
            CameraToVLPose(Camera camera, Matrix4x4 offset, out Vector4 t, out Quaternion q)
        {
            Matrix4x4 worldToCameraMatrix = offset * camera.worldToCameraMatrix;
            WorldToCameraMatrixToVLPose(worldToCameraMatrix, out t, out q);
        }

        public static void WorldToCameraMatrixToVLPose(
            Matrix4x4 worldToCameraMatrix,
            out Vector4 t,
            out Quaternion q)
        {
            worldToCameraMatrix *= CameraHelper.rotationY180; // Add 180 degree rotation

            // Convert from left-handed to right-handed model-view matrix
            worldToCameraMatrix[0, 2] = -worldToCameraMatrix[0, 2];
            worldToCameraMatrix[1, 2] = -worldToCameraMatrix[1, 2];
            worldToCameraMatrix[2, 2] = -worldToCameraMatrix[2, 2];

            // Convert from OpenGL coordinates into VisionLib coordinates
            worldToCameraMatrix = CameraHelper.flipYZ * worldToCameraMatrix;

            t = worldToCameraMatrix.GetColumn(3);
            q = QuaternionFromMatrix(worldToCameraMatrix);
        }

        /// <summary>
        /// Translates position and rotation from Unity in VL coordinate system (in place).
        /// </summary>
        /// <param name="postion"> The position in Unity coordinates.
        /// <param name="rotation"> The rotation in Unity coordinates.
        public static void ToVLInPlace(ref Vector3 position, ref Quaternion rotation)
        {
            rotation.z *= -1;
            rotation.y *= -1;
            position.x *= -1;
        }

        /// <summary>
        /// Takes the local transform of a GameObject and transforms it to a valid
        /// Workspace.Transform.
        /// </summary>
        /// <param name="geometry">GameObject, which local transform should be used.</param>
        /// <param name="globalTransform">if true use global, if false use local transform.</param>
        public static Core.API.WorkSpace.Transform
            CreateVLTransform(GameObject geometry, bool globalTransform)
        {
            Quaternion q;
            Vector3 t;
            if (globalTransform)
            {
                q = geometry.transform.rotation;
                t = geometry.transform.position;
            }
            else
            {
                q = geometry.transform.localRotation;
                t = geometry.transform.localPosition;
            }

            ToVLInPlace(ref t, ref q);

            return new Core.API.WorkSpace.Transform(t, q);
        }

        /// <summary>
        /// Takes a vector3 from the vlSDK and transforms it to a vector in the Unity coordinate
        /// system.
        /// </summary>
        /// <param name="vlVector"></param>
        /// <returns></returns>
        public static Vector3 UnityVectorToVLVector(Vector3 vlVector)
        {
            vlVector.x *= -1;
            return vlVector;
        }

        public static Vector3 VLPoseToUnityPose(Vector3 t)
        {
            t.x *= -1;
            return t;
        }

        public static ModelTransform
            UnityToVLPose(ModelTransform mt)
        {
            Vector3 newPosition = mt.t;
            Quaternion newRotation = mt.q;
            newPosition.y *= -1;
            var tmp = newRotation.x;
            newRotation.x = newRotation.y;
            newRotation.y = tmp;
            tmp = newRotation.z;
            newRotation.z = newRotation.w;
            newRotation.w = tmp;
            return new ModelTransform(newRotation, newPosition);
        }

        /// <summary>
        ///  Computes the position and rotation of a Unity Camera object from a
        ///  VisionLib pose.
        /// </summary>
        /// <param name="t">
        ///  Translation in VisionLib coordinates.
        /// </param>
        /// <param name="q">
        ///  Rotation in VisionLib coordinates.
        /// </param>
        /// <param name="position">
        ///  Position in Unity coordinates.
        /// </param>
        /// <param name="orientation">
        ///  Rotation in Unity coordinates.
        /// </param>
        public static void VLPoseToCamera(
            Vector3 t,
            Quaternion q,
            out Vector3 position,
            out Quaternion orientation)
        {
            // Add 180 degree rotation around the y axis
            q *= Quaternion.Euler(0, 180f, 0);

            position = -(Quaternion.Inverse(q) * t);
            // Negate the z-component in order to convert the right-handed
            // translation into a left-handed translation
            position.z = -position.z;

            // Rotate 180 degree around the x-axis in order to convert
            // the rotation from VisionLib coordinates
            // (x: right, y: down, z: inside) to right-handed
            // coordinates (x: right, y: up: z: outside)
            // Quaternion rotX180 = Quaternion.AngleAxis(180, Vector3.right);
            // q = q * rotX180;
            q = new Quaternion(q.w, q.z, -q.y, -q.x);

            // Negate the x- and z-component in order to convert the
            // right-handed rotation into a left-handed rotation
            // (negating the y- and w-component would have the same
            // effect)
            q.x = -q.x;
            q.z = -q.z;

            // Invert the rotation, because we want the rotation of the camera in
            // the world and not the rotation of the world around the camera
            orientation = Quaternion.Inverse(q);
        }

        // Given a ScreenOrientation, returns an enum value describing how the
        // video image has to be rotated to match.
        public static RenderRotation GetRenderRotation(ScreenOrientation orientation)
        {
#if ((UNITY_WSA_10_0 || UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            // On UWP, Android and iOS, the default screen orientation is landscape.
            switch (orientation)
            {
                case ScreenOrientation.LandscapeLeft:
                default:
                    return RenderRotation.CCW0;
                case ScreenOrientation.PortraitUpsideDown:
                    return RenderRotation.CCW90;
                case ScreenOrientation.LandscapeRight:
                    return RenderRotation.CCW180;
                case ScreenOrientation.Portrait:
                    return RenderRotation.CCW270;
                case ScreenOrientation.AutoRotation:
                    LogHelper.LogWarning("AutoRotation is not supported");
                    return RenderRotation.CCW0;
            }
#else
            // On the desktop the default screen orientation in Unity is portrait.
            switch (orientation)
            {
                case ScreenOrientation.Portrait:
                default:
                    return RenderRotation.CCW0;
                case ScreenOrientation.LandscapeLeft:
                    return RenderRotation.CCW90;
                case ScreenOrientation.PortraitUpsideDown:
                    return RenderRotation.CCW180;
                case ScreenOrientation.LandscapeRight:
                    return RenderRotation.CCW270;
                case ScreenOrientation.AutoRotation:
                    LogHelper.LogWarning("AutoRotation is not supported");
                    return RenderRotation.CCW0;
            }
#endif
            throw new System.ArgumentException("Enum value not in enum", "orientation");
        }

        public static Matrix4x4 GetRenderRotationMatrixFromVLToUnity(RenderRotation rotation)
        {
            switch (rotation)
            {
                case RenderRotation.CCW0:
                    return rotationZ0;
                case RenderRotation.CCW90:
                    return rotationZ90;
                case RenderRotation.CCW180:
                    return rotationZ180;
                case RenderRotation.CCW270:
                    return rotationZ270;
            }
            throw new System.ArgumentException("Enum value not in enum", "orientation");
        }

        public static Matrix4x4 GetRenderRotationMatrixFromUnityToVL(RenderRotation rotation)
        {
            switch (rotation)
            {
                case RenderRotation.CCW0:
                    return rotationZ0;
                case RenderRotation.CCW90:
                    return rotationZ270;
                case RenderRotation.CCW180:
                    return rotationZ180;
                case RenderRotation.CCW270:
                    return rotationZ90;
            }
            throw new System.ArgumentException("Enum value not in enum", "orientation");
        }

        public static void SetUnityTransformTo(Transform tUnity, ModelTransform tVL)
        {
            Vector3 newPosition;
            Quaternion newRotation;
            SetUnityRotationTranslationTo(out newRotation, out newPosition, tVL);
            tUnity.localRotation = newRotation;
            tUnity.localPosition = newPosition;
        }

        public static void SetUnityRotationTranslationTo(
            out Quaternion rotationOut,
            out Vector3 positionOut,
            ModelTransform tVL)
        {
            Matrix4x4 localTransformMatrix =
                flipX *
                Matrix4x4.TRS(
                    new Vector3(tVL.t[0], tVL.t[1], tVL.t[2]),
                    new Quaternion(tVL.q[0], tVL.q[1], tVL.q[2], tVL.q[3]),
                    new Vector3(tVL.s[0], tVL.s[1], tVL.s[2])) *
                flipX;

            rotationOut = CameraHelper.QuaternionFromMatrix(localTransformMatrix);
            positionOut = localTransformMatrix.GetColumn(3);
        }

        /// <summary>
        /// Transforms a ModelViewMatrix in VisionLib coordinate system to a position and a rotation
        /// usable in Unity transformations.
        /// </summary>
        /// <param name="modelViewMatrix">ModelViewMatrix in VisionLib coordinate system</param>
        /// <param name="renderRotationMatrixFromVLToUnity">The renderRotationMatrixFromVLToUnity
        /// reverses the rotation of the scene which Unity does automatically, if the screen was
        /// rotated.</param> <param name="position">Out parameter for position of the
        /// transform</param> <param name="rotation">Out parameter for rotation of the
        /// transform</param>
        public static void ModelViewMatrixToUnityPose(
            Matrix4x4 modelViewMatrix,
            Matrix4x4 renderRotationMatrixFromVLToUnity,
            out Vector3 position,
            out Quaternion rotation)
        {
            // Compute the right-handed world to camera matrix
            // (the inverseCameraRotationMatrix multiplication reverses the rotation of the scene,
            // which Unity does automatically, if the screen was rotated)
            Matrix4x4 worldToCameraMatrix = renderRotationMatrixFromVLToUnity * modelViewMatrix;

            // XZ Flip
            worldToCameraMatrix *= CameraHelper.rotationY180;

            // Compute the left-handed world to camera matrix
            worldToCameraMatrix = CameraHelper.flipZ * worldToCameraMatrix * CameraHelper.flipZ;

            // Compute the left-handed camera to world matrix
            Matrix4x4 cameraToWorldMatrix = worldToCameraMatrix.inverse;

            // Extract the rotation and translation from the computed matrix
            rotation = Quaternion.LookRotation(
                cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
            position = cameraToWorldMatrix.GetColumn(3);
        }

        public static ModelTrackerCommands.InitPose TransformToGlobalObjectPose(Transform transform)
        {
            Matrix4x4 globalObjectMatrix = transform.localToWorldMatrix;
            globalObjectMatrix =
                CameraHelper.flipXY * CameraHelper.flipX * globalObjectMatrix * CameraHelper.flipX;

            Vector3 t = globalObjectMatrix.GetColumn(3);
            Quaternion q = Quaternion.LookRotation(
                globalObjectMatrix.GetColumn(2), globalObjectMatrix.GetColumn(1));

            return new ModelTrackerCommands.InitPose(t, q);
        }
    }
}
