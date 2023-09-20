using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.API.Native;
using Visometry.VisionLib.SDK.Core.Details;
#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace Visometry.VisionLib.SDK.Core
{
    /**
     *  @ingroup Core
     */
    public interface InputFrame : IDisposable
    {
        Frame Evaluate();
    }

    /**
     *  @ingroup Core
     */
    [AddComponentMenu("VisionLib/Core/Synchronous Tracking Manager")]
    public class SynchronousTrackingManager : TrackingManager
    {
        private class FrameBuffer<FrameType> : IDisposable where FrameType : class, IDisposable
        {
            private List<FrameType> frames = new List<FrameType>();
            private const int maxSize = 2;
            private bool disposed = false;
            private object framesLock = new object();

            public void Push(FrameType frame)
            {
                FrameType frameToDispose = null;
                lock(this.framesLock)
                {
                    if (this.frames.Count == maxSize)
                    {
                        frameToDispose = this.frames[0];
                        this.frames.RemoveAt(0);
                    }
                    this.frames.Add(frame);
                }
                if (frameToDispose != null)
                {
                    frameToDispose.Dispose();
                }
            }

            public FrameType Pop()
            {
                FrameType fetchedFrame = null;
                lock(this.framesLock)
                {
                    if (this.frames.Count > 0)
                    {
                        fetchedFrame = this.frames[0];
                        this.frames.RemoveAt(0);
                    }
                }
                return fetchedFrame;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (this.disposed)
                {
                    return;
                }

                if (disposing)
                {
                    lock(this.framesLock)
                    {
                        foreach (var frame in this.frames)
                        {
                            frame.Dispose();
                        }
                        this.frames.Clear();
                    }
                }
                this.disposed = true;
            }
        }

#if NETFX_CORE
        private class MyThread
        {
            private Task task;
            public MyThread(Action run)
            {
                this.task = new Task(run);
                this.task.Start();
            }
            public void Join()
            {
                this.task.Wait();
            }
        }
#else
        private class MyThread
        {
            private Thread thread;

            public MyThread(ThreadStart run)
            {
                this.thread = new Thread(run);
                this.thread.Start();
            }
            public void Join()
            {
                this.thread.Join();
            }
        }
#endif

        private MyThread workerThread;

        private FrameBuffer<InputFrame> frameToInject = new FrameBuffer<InputFrame>();
        private FrameBuffer<Frame> trackingResult = new FrameBuffer<Frame>();
        private bool exitThread = true;

        public override void StartTracking(string path)
        {
            StopTracking();
            this.exitThread = false;
            base.StartTracking(path);
            StartWorkerThread();
        }

        private void StartWorkerThread()
        {
            this.workerThread = new MyThread(WorkerThreadRun);
        }

        private void StopWorkerThread()
        {
            this.exitThread = true;
            this.workerThread.Join();
            this.workerThread = null;
        }

        private void WorkerThreadRun()
        {
            while (!GetTrackerInitialized() && !this.exitThread)
            {
                this.RunOnceSync();
            }
            while (GetTrackerInitialized() && !this.exitThread)
            {
                WorkerThreadApply();
            }
        }

        private void WorkerThreadApply()
        {
            using(var frameToInject = this.frameToInject.Pop())
            {
                if (frameToInject == null)
                {
                    return;
                }
                try
                {
                    using (var frameToTrack = frameToInject.Evaluate())
                    {
                        if (frameToTrack == null)
                        {
                            return;
                        }
                        using (var newTrackingResult = this.TrackFrame(frameToTrack))
                        {
                            if (newTrackingResult != null)
                            {
                                this.trackingResult.Push(newTrackingResult.Clone());
                            }
                        }
                    }
                }
                catch (InvalidOperationException e)
                {
                    LogHelper.LogException(e);
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!GetTrackerInitialized())
            {
                return;
            }

            using(var trackingResult = this.trackingResult.Pop())
            {
                if (trackingResult != null)
                {
                    TrackingManager.EmitEvents(trackingResult);
                }
            }
        }

        public override void StopTracking()
        {
            if (this.workerThread != null)
            {
                StopWorkerThread();
                base.StopTracking();
                this.frameToInject.Dispose();
                this.trackingResult.Dispose();
            }
        }

        protected override void OnDestroy()
        {
            StopTracking();
            base.OnDestroy();
        }

        public void Push(InputFrame frame)
        {
            this.frameToInject.Push(frame);
        }

        public bool IsReady()
        {
            return GetTrackerInitialized();
        }

        protected override void CreateWorker()
        {
            this.worker = new Worker(true);
        }

        protected override void RegisterListeners() {}

        protected override void UnregisterListeners() {}

        protected override void RegisterTrackerListeners() {}

        protected override void UnregisterTrackerListeners() {}

        protected override void UpdateAnchorTransformListeners() {}

        public bool RunOnceSync()
        {
            return this.Worker.RunOnceSync();
        }

        public Frame TrackFrame(Frame frame)
        {
            lock(this.workerLock)
            {
                if (!this.Worker.IsRunning())
                {
                    return null;
                }

                this.Worker.SetNodeImageSync(frame.image, "inject0", "imageIn");
                this.Worker.SetNodeIntrinsicDataSync(frame.intrinsicData, "inject0", "intrinsic");
                this.Worker.SetNodeExtrinsicDataSync(frame.extrinsicData, "inject0", "extrinsic");

                RunOnceSync();

                Frame result = new Frame();
                foreach (var anchorName in anchorObservableMap.GetAnchorNames())
                {
                    var transform = this.Worker.GetNodeSimilarityTransformSync(
                        "", "WorldFrom" + anchorName + "Transform");
                    result.anchorTransforms[anchorName] = transform;
                }

                result.image = this.Worker.GetNodeImageSync("", "image");
                result.intrinsicData =
                    this.Worker.GetNodeIntrinsicDataSync("inject0", "intrinsicDisplay");
                result.cameraTransform =
                    this.Worker.GetNodeExtrinsicDataSync("", "worldFromCameraTransform");
                result.extrinsicData = this.Worker.GetNodeExtrinsicDataSync("", "extrinsic");
                result.trackingState = this.Worker.GetNodeTrackingStateSync("");
                return result;
            }
        }
    }
}
