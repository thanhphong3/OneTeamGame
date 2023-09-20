using UnityEngine;
using System;

namespace Visometry.VisionLib.SDK.Core.Details
{
    public class TransformCache
    {
        private Quaternion rotation;
        private Vector3 position;
        private bool valid = false;
        private Action changeListener;

        public TransformCache(Action changeListener)
        {
            this.changeListener = changeListener;
        }

        private bool Equals(Transform trans)
        {
            return this.valid && this.rotation == trans.rotation && this.position == trans.position;
        }

        public void Write(Transform trans)
        {
            if (Equals(trans))
            {
                return;
            }
            this.changeListener();
            this.rotation = trans.rotation;
            this.position = trans.position;
            this.valid = true;
        }

        public void Invalidate()
        {
            this.valid = false;
        }
    }
}
