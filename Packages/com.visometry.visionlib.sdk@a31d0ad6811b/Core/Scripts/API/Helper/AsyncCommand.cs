using AOT;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Visometry.VisionLib.SDK.Core.Details;
using Visometry.VisionLib.SDK.Core.API.Native;

namespace Visometry.VisionLib.SDK.Core.API
{
    /// @ingroup API
    internal class AsyncCommand
    {
        private TaskCompletionSource<string> t = new TaskCompletionSource<string>();
        private GCHandle gcHandle;

        private AsyncCommand(Worker worker, WorkerCommands.CommandBase cmd)
        {
            this.gcHandle = GCHandle.Alloc(this);
            if (!worker.PushCommand(cmd, commandCallbackDelegate, GCHandle.ToIntPtr(this.gcHandle)))
            {
                throw new InvalidOperationException("Could not send command.");
            }
        }

        private Task<string> GetTask()
        {
            return t.Task;
        }

        private static AsyncCommand GetInstance(IntPtr clientData)
        {
            return (AsyncCommand) GCHandle.FromIntPtr(clientData).Target;
        }

        [MonoPInvokeCallback(typeof(Worker.JsonStringCallback))]
        private static void DispatchCallback(string errorJson, string resultJson, IntPtr clientData)
        {
            try
            {
                GetInstance(clientData).Callback(errorJson, resultJson);
            }
            catch (Exception e) // Catch all exceptions, because this is a callback
                                // invoked from native code
            {
                LogHelper.LogException(e);
            }
        }

        private static Worker.JsonStringCallback commandCallbackDelegate =
            new Worker.JsonStringCallback(DispatchCallback);

        private void Callback(string errorJson, string resultJson)
        {
            try
            {
                if (errorJson != null)
                {
                    var error = JsonHelper.FromJson<WorkerCommands.CommandError>(errorJson);
                    if (error.IsCanceled())
                    {
                        LogHelper.LogDebug(
                            "'" + error.commandName +
                            "' has been canceled because the tracker has been stopped or destroyed.");
                        t.SetCanceled();
                    }
                    else
                    {
                        t.SetException(error);
                    }
                }
                else
                {
                    t.SetResult(resultJson);
                }
            }
            catch (Exception e)
            {
                if (e != null)
                {
                    t.TrySetException(e);
                }
            }
            finally
            {
                this.gcHandle.Free();
            }
        }

        public static Task<string> Execute(Worker worker, WorkerCommands.CommandBase cmd)
        {
            return (new AsyncCommand(worker, cmd)).GetTask();
        }
    }
}
