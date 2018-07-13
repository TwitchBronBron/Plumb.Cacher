using System;
using System.Reflection;
using System.Threading;

namespace Plumb.Cacher
{
    public static class ThreadExtensions
    {

        private static MethodInfo GetAbortMethod<ThreadType>()
        {
            return GetAbortMethod(typeof(ThreadType));
        }
        private static MethodInfo GetAbortMethod(Type threadType)
        {
            MethodInfo abort = null;
            foreach (var m in threadType.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
            }
            return abort;
        }

        /// <summary>
        /// Determine whether or not this runtime environment supports Thread.Abort.
        /// </summary>
        /// <returns></returns>
        public static bool ThreadSupportsAbort()
        {
            return GetAbortMethod<Thread>() != null;
        }


        /// <summary>
        /// Attempt to abort a thread. If the thread cannot be aborted, throw an exception on THIS thread.
        /// </summary>
        /// <param name="thread"></param>
        public static void AbortSafe(this Thread thread, object[] data)
        {
            var abortMethod = GetAbortMethod(thread.GetType());
            if (abortMethod != null)
            {
                abortMethod.Invoke(thread, data);
            }
            else
            {
                throw new Exception($"Unable to abort thread with id {thread.ManagedThreadId}.");
            }
        }
    }
}