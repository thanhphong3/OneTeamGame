using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// @ingroup API
    [System.Serializable]
    public struct ModelTransform
    {
        public Vector3 t;
        public Vector3 s;
        public Quaternion q;

        public static bool operator ==(ModelTransform a, ModelTransform b)
        {
            var translation = (a.t - b.t).magnitude;
            float angle;
            (a.q * Quaternion.Inverse(b.q)).ToAngleAxis(out angle, out _);
            return translation < 0.01 && (angle < 0.1 || angle > 359.9);
        }

        public static bool operator !=(ModelTransform a, ModelTransform b)
        {
            return !(a == b);
        }

        public ModelTransform(Matrix4x4 m) :
            this(CameraHelper.QuaternionFromMatrix(m), m.GetColumn(3))
        {
        }

        public ModelTransform(Transform unityTransform) :
            this(unityTransform.rotation, unityTransform.position)
        {
        }

        public static ModelTransform operator*(Matrix4x4 left, ModelTransform right)
        {
            return new ModelTransform(left * right.ToMatrix());
        }

        public static ModelTransform operator*(ModelTransform left, Matrix4x4 right)
        {
            return new ModelTransform(left.ToMatrix() * right);
        }

        public static ModelTransform operator*(ModelTransform left, ModelTransform right)
        {
            return left.ToMatrix() * right;
        }

        public ModelTransform(SimilarityTransform similarityTransform)
        {
            this.q = similarityTransform.GetR();
            this.q.Normalize();
            this.t = similarityTransform.GetT();
            this.s = Vector3.one * similarityTransform.GetS();
        }

        public ModelTransform(ExtrinsicData extrinsicData)
        {
            this.q = extrinsicData.GetR();
            this.q.Normalize();
            this.t = extrinsicData.GetT();
            this.s = Vector3.one;
        }

        public ModelTransform(Quaternion qIn, Vector3 tIn)
        {
            this.q = qIn;
            this.t = tIn;
            this.s = Vector3.one;
        }

        public ModelTransform(Transform transform, Transform rootTransform)
        {
            this.q = transform.rotation;
            this.t = transform.position;
            this.s = GetGlobalScale(transform);

            // On HoloLens, the content node is added to the camera and thus the
            // transformation of the mesh will be changed. This change has to be
            // removed when streaming the data into the vlSDK
            if (rootTransform != null)
            {
                Vector3 contentGlobalScale = GetGlobalScale(rootTransform);
                this.q = Quaternion.Inverse(rootTransform.rotation) * this.q;
                this.s = new Vector3(
                    this.s.x / contentGlobalScale.x,
                    this.s.y / contentGlobalScale.y,
                    this.s.z / contentGlobalScale.z);
                this.t =
                    Quaternion.Inverse(rootTransform.rotation) * (this.t - rootTransform.position);
            }

            CameraHelper.ToVLInPlace(ref this.t, ref this.q);
        }

        public ModelTransform(ModelTrackerCommands.InitPose initPose)
        {
            this.q = new Quaternion(initPose.q[0], initPose.q[1], initPose.q[2], initPose.q[3]);
            this.t = new Vector3(initPose.t[0], initPose.t[1], initPose.t[2]);
            this.s = Vector3.one;

            this = CameraHelper.flipXY * this;
        }

        public static ModelTransform Identity()
        {
            ModelTransform mt = new ModelTransform();
            mt.q = Quaternion.identity;
            mt.t = Vector3.zero;
            mt.s = Vector3.one;
            return mt;
        }

        public bool IsFarAway()
        {
            return this.t.x > 100000.0 || this.t.x < -100000.0 || this.t.y > 100000.0 ||
                   this.t.y < -100000.0 || this.t.z > 100000.0 || this.t.z < -100000.0;
        }

        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.TRS(t, q, s);
        }

        public ModelTransform Inverse()
        {
            return new ModelTransform(this.ToMatrix().inverse);
        }

        /// <summary>
        /// Multiplies all scales of all hierarchy levels
        /// </summary>
        private static Vector3 GetGlobalScale(Transform transform)
        {
            if (transform.parent)
            {
                Vector3 combinedScale = GetGlobalScale(transform.parent);
                combinedScale.x *= transform.localScale.x;
                combinedScale.y *= transform.localScale.y;
                combinedScale.z *= transform.localScale.z;
                return combinedScale;
            }

            return transform.localScale;
        }

        [System.Obsolete("ModelTransform.multiplyLeft is obsolete. Please use operator * instead.")]
        public ModelTransform multiplyLeft(Matrix4x4 m)
        {
            return m * this;
        }

        public override bool Equals(object obj)
        {
            return obj is ModelTransform transform && transform == this;
        }

        public override int GetHashCode()
        {
            int hashCode = 2114140033;
            hashCode = hashCode * -1521134295 + t.GetHashCode();
            hashCode = hashCode * -1521134295 + s.GetHashCode();
            hashCode = hashCode * -1521134295 + q.GetHashCode();
            return hashCode;
        }
    }
}
