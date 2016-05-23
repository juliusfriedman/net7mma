#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion
namespace Media.Concepts.Classes.Threading
{
    /// <summary>
    /// <see cref="System.Byte"/> representation of <see cref="System.Threading.ThreadPriority"/>
    /// </summary>
    public enum /*Byte*/ThreadPriority : byte
    {
        AboveNormal = System.Threading.ThreadPriority.AboveNormal,
        BelowNormal = System.Threading.ThreadPriority.BelowNormal,
        Highest = System.Threading.ThreadPriority.Highest,
        Lowest = System.Threading.ThreadPriority.Lowest,
        Normal = System.Threading.ThreadPriority.Normal
    }

    /// <summary>
    /// Contains methods for setting Thread Priority via stored values.
    /// Overlaps 4 <see cref="ThreadPriority"/> structures in one using the same amount of memory.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public struct ThreadPriorityInformation
    {
        #region Fields
        //4 bytes
        [System.Runtime.InteropServices.FieldOffset(0)]
        public System.Threading.ThreadPriority OverlappedThreadPriority;

        //----
        [System.Runtime.InteropServices.FieldOffset(0)]
        public ThreadPriority StartPriority;

        [System.Runtime.InteropServices.FieldOffset(1)]
        public ThreadPriority IdlePriority;

        [System.Runtime.InteropServices.FieldOffset(2)]
        public ThreadPriority RunningPriority;

        [System.Runtime.InteropServices.FieldOffset(3)]
        public ThreadPriority AbortPriority;
        #endregion

        #region Unused

        //---

        //DateTime Started, Aborted

        //---

        #endregion

        #region Methods

        /// <summary>
        /// Sets the Priority to <see cref="StartPriority"/> and calls Start
        /// </summary>
        /// <param name="thread"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Start(System.Threading.Thread thread)
        {
            if (thread == null || thread.IsAlive) return;

            thread.Priority = (System.Threading.ThreadPriority)StartPriority;

            thread.Start();

            thread.Priority = (System.Threading.ThreadPriority)RunningPriority;
        }


        /// <summary>
        /// Sets the Priority to <see cref="AbortPriority"/> and call Abort
        /// </summary>
        /// <param name="thread"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void RaiseAbort(System.Threading.Thread thread)
        {
            if (thread == null || thread.ThreadState.HasFlag(System.Threading.ThreadState.Aborted) || thread.ThreadState.HasFlag(System.Threading.ThreadState.AbortRequested)) return;

            thread.Priority = (System.Threading.ThreadPriority)AbortPriority;

            try { thread.Abort(); }
            catch (System.Threading.ThreadAbortException) { System.Threading.Thread.ResetAbort(); }
            catch { throw; }
        }

        /// <summary>
        /// Sets the Priority and calls Join.
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="priority"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool JoinFor(System.Threading.Thread thread, System.Threading.ThreadPriority priority, System.TimeSpan timeout)
        {
            if (thread == null || thread.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin)) return false;

            System.Threading.ThreadPriority previous = thread.Priority;

            thread.Priority = priority;

            try { return thread.Join(timeout); }
            catch { throw; }
            finally { thread.Priority = previous; }
        }

        /// <summary>
        /// Sets the Priority to <see cref="IdlePriority"/> and call Sleep
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="timeout"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SleepFor(System.Threading.Thread thread, System.TimeSpan timeout, System.Threading.ThreadPriority priority)
        {
            if (thread == null || thread.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin)) return;

            System.Threading.ThreadPriority previous = thread.Priority;

            thread.Priority = priority;

            System.Threading.Thread.Sleep(timeout);

            thread.Priority = previous;
        }

        public void SleepFor(System.Threading.Thread thread, System.TimeSpan timeout)
        {
            SleepFor(thread, timeout, (System.Threading.ThreadPriority)IdlePriority);
        }

        /// <summary>
        /// Sets the Priority to <see cref="IdlePriority"/> 
        /// </summary>
        /// <param name="thread"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SetIdle(System.Threading.Thread thread)
        {
            if (thread == null || false == thread.IsAlive) return;

            thread.Priority = (System.Threading.ThreadPriority)IdlePriority;
        }

        /// <summary>
        /// Sets the Priority
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="priority"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SetRunning(System.Threading.Thread thread, System.Threading.ThreadPriority priority)
        {
            if (thread == null || false == thread.IsAlive) return;

            thread.Priority = priority;
        }

        /// <summary>
        /// Sets the Priority to <see cref="RunningPriority"/> 
        /// </summary>
        /// <param name="thread"></param>
        public void SetRunning(System.Threading.Thread thread) { SetRunning(thread, (System.Threading.ThreadPriority)RunningPriority); }

        //Interrupts

        #endregion
    }

    /// <summary>
    /// Thread safe event invoker
    /// </summary>
    /// <remarks>
    /// <see href="http://stackoverflow.com/questions/786383/c-sharp-events-and-thread-safety">Stack Overflow</see>
    /// </remarks>
    public sealed class ThreadSafeEventInvoker
    {
        /// <summary>
        /// Dictionary of delegates
        /// </summary>
        readonly System.Collections.Concurrent.ConcurrentDictionary<System.Delegate, DelegateHolder> DelegateDictionary = new System.Collections.Concurrent.ConcurrentDictionary<System.Delegate, DelegateHolder>();

        /// <summary>
        /// List of delegates to be called, we need it because it is relatevely easy to implement a loop with list
        /// modification inside of it
        /// </summary>
        readonly System.Collections.Generic.LinkedList<DelegateHolder> DelegateList = new System.Collections.Generic.LinkedList<DelegateHolder>();

        /// <summary>
        /// locker for delegates list
        /// </summary>
        private readonly System.Threading.ReaderWriterLockSlim ReadWriteLock = new System.Threading.ReaderWriterLockSlim();

        /// <summary>
        /// Add delegate to list
        /// </summary>
        /// <param name="value"></param>
        public void Add(System.Delegate value)
        {
            DelegateHolder Holder = new DelegateHolder(value);

            if (false == DelegateDictionary.TryAdd(value, Holder)) return;

            ReadWriteLock.EnterWriteLock();

            DelegateList.AddLast(Holder);

            ReadWriteLock.ExitWriteLock();
        }

        /// <summary>
        /// Remove delegate from list
        /// </summary>
        /// <param name="value"></param>
        public void Remove(System.Delegate value)
        {
            DelegateHolder holder;

            if (false == DelegateDictionary.TryRemove(value, out holder)) return;

            System.Threading.Monitor.Enter(holder);

            holder.IsDeleted = true;

            System.Threading.Monitor.Exit(holder);
        }

        /// <summary>
        /// Raise an event
        /// </summary>
        /// <param name="args"></param>
        public void Raise(params object[] args)
        {
            DelegateHolder Holder = null;

            try
            {
                // get root element
                ReadWriteLock.EnterReadLock();

                System.Collections.Generic.LinkedListNode<DelegateHolder> Cursor = DelegateList.First;

                ReadWriteLock.ExitReadLock();

                while (Cursor != null)
                {
                    // get its value and a next node
                    ReadWriteLock.EnterReadLock();

                    Holder = Cursor.Value;

                    System.Collections.Generic.LinkedListNode<DelegateHolder> Next = Cursor.Next;

                    ReadWriteLock.ExitReadLock();

                    // lock holder and invoke if it is not removed
                    System.Threading.Monitor.Enter(Holder);
                    
                    if (false == Holder.IsDeleted) Holder.Action.DynamicInvoke(args);
                    else if (false == Holder.IsDeletedFromList)
                    {
                        ReadWriteLock.EnterWriteLock();

                        DelegateList.Remove(Cursor);

                        Holder.IsDeletedFromList = true;

                        ReadWriteLock.ExitWriteLock();
                    }

                    System.Threading.Monitor.Exit(Holder);

                    Cursor = Next;
                }
            }
            catch
            {
                // clean up
                if (ReadWriteLock.IsReadLockHeld) ReadWriteLock.ExitReadLock();

                if (ReadWriteLock.IsWriteLockHeld) ReadWriteLock.ExitWriteLock();

                if (Holder != null && System.Threading.Monitor.IsEntered(Holder)) System.Threading.Monitor.Exit(Holder);

                throw;
            }
        }

        /// <summary>
        /// helper class
        /// </summary>
        internal class DelegateHolder
        {
            /// <summary>
            /// delegate to call
            /// </summary>
            public System.Delegate Action { get; private set; }

            /// <summary>
            /// flag shows if this delegate removed from list of calls
            /// </summary>
            public bool IsDeleted { get; set; }

            /// <summary>
            /// flag shows if this instance was removed from all lists
            /// </summary>
            public bool IsDeletedFromList { get; set; }

            /// <summary>
            /// Constuctor
            /// </summary>
            /// <param name="d"></param>
            public DelegateHolder(System.Delegate d)
            {
                Action = d;
            }
        }
    }

    //Todo, benchmark and determine how useful a more efficient lock is
    //https://github.com/i255/ReaderWriterLockTiny/blob/master/ReaderWriterLockTiny/ReaderWriterLockTiny.cs
}
