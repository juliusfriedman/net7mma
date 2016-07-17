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
        /// <summary>
        /// 4 bytes which are used to store various <see cref="System.Threading.ThreadPriority"/>
        /// </summary>        
        [System.Runtime.InteropServices.FieldOffset(0)]
        internal int Flags;

        /// <summary>
        /// 4 bytes which are used to store various <see cref="System.Threading.ThreadPriority"/>
        /// </summary>
        [System.Runtime.InteropServices.FieldOffset(4)]
        internal int Version;

        #region Properties

        //---- @ 0

        [System.Runtime.InteropServices.FieldOffset(0)]
        public System.Threading.ThreadPriority OverlappedThreadPriority;

        //---- @ 0
        
        [System.Runtime.InteropServices.FieldOffset(0)]
        public ThreadPriority StartPriority;

        [System.Runtime.InteropServices.FieldOffset(1)]
        public ThreadPriority IdlePriority;

        [System.Runtime.InteropServices.FieldOffset(2)]
        public ThreadPriority RunningPriority;

        [System.Runtime.InteropServices.FieldOffset(3)]
        public ThreadPriority AbortPriority;

        //---- @ 4
        
        [System.Runtime.InteropServices.FieldOffset(4)]
        public System.Threading.ThreadPriority OverlappedThreadPriority2;

        //---- @ 4

        [System.Runtime.InteropServices.FieldOffset(4)]
        public ThreadPriority ExceptionPriority;

        [System.Runtime.InteropServices.FieldOffset(5)]
        public ThreadPriority EndPriority;

        [System.Runtime.InteropServices.FieldOffset(6)]
        public ThreadPriority SleepWaitJoinPriority;

        [System.Runtime.InteropServices.FieldOffset(7)]
        public ThreadPriority InterruptPriority;
        #endregion

        #region Unused

        //---

        //DateTime Started, Aborted

        //---

        #endregion

        //Todo, move to extensions.

        #region Methods

        /// <summary>
        /// Sets the Priority to <see cref="StartPriority"/> and calls Start
        /// </summary>
        /// <param name="thread"></param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Start(System.Threading.Thread thread)
        {
            if (object.ReferenceEquals(thread, null) || thread.IsAlive) return;

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
            if (object.ReferenceEquals(thread, null) || thread.ThreadState.HasFlag(System.Threading.ThreadState.Aborted) || thread.ThreadState.HasFlag(System.Threading.ThreadState.AbortRequested)) return;

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
            if (object.ReferenceEquals(thread, null) || thread.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin)) return false;

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
            if (object.ReferenceEquals(thread, null) || thread.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin)) return;

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
            if (object.ReferenceEquals(thread, null) || false.Equals(thread.IsAlive)) return;

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
            if (object.ReferenceEquals(thread, null) || false.Equals(thread.IsAlive)) return;

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

    //ManualResetEvent
    //public sealed class ResetEvent
    //{

    //}

    public class EnumerableException : Common.TaggedException<Fiber>, System.Collections.Generic.IEnumerable<System.Exception>
    {
        const System.Exception NilException = null;

        System.Collections.Generic.IEnumerable<System.Exception> Aggregates = System.Linq.Enumerable.Empty<System.Exception>();

        readonly System.Collections.Generic.HashSet<System.Func<System.Exception, bool>> ExceptionHandlers = new System.Collections.Generic.HashSet<System.Func<System.Exception, bool>>();

        public EnumerableException()
        {

        }

        public bool AddHandler(System.Func<System.Exception, bool> handler)
        {
            return ExceptionHandlers.Add(handler);
        }

        public bool RemoveHandler(System.Func<System.Exception, bool> handler)
        {
            return ExceptionHandlers.Remove(handler);
        }

        public void Store(System.Exception e)
        {
            if (object.ReferenceEquals(e, NilException)) return;

            Aggregates = System.Linq.Enumerable.Concat(Aggregates, Media.Common.Extensions.Linq.LinqExtensions.Yield(e));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns>True when handled, otherwise false</returns>
        public bool Handle(System.Exception e)
        {
            if (object.ReferenceEquals(e, NilException)) return true;

            foreach (var ExceptionHandler in ExceptionHandlers)
            {
                if (ExceptionHandler(e))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True when an unhandled <see cref="System.Exception"/> occurs</returns>
        public bool Handle(out System.Exception exception)
        {
            foreach (System.Exception Exception in Aggregates)
            {
                if (Handle(Exception)) continue;

                exception = Exception;

                return false;
            }

            exception = null;

            return true;
        }

        //public static implicit operator System.Collections.Generic.IEnumerable<System.Exception>(AggregateException aggregate){
        //    return aggregate.Aggregates;
        //}

        public System.Collections.Generic.IEnumerator<System.Exception> GetExceptions()
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(this)) throw new System.ObjectDisposedException("The instance is disposed.", this);

            return Aggregates.GetEnumerator();
        }

        System.Collections.Generic.IEnumerator<System.Exception> System.Collections.Generic.IEnumerable<System.Exception>.GetEnumerator()
        {
            return GetExceptions();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetExceptions();
        }
    }

    /// <summary>
    /// An implementation which utilizes a single <see cref="System.Threading.Thread"/> to perform multiple <see cref="System.Action"/>'s with specified <see cref="ThreadPriorityInformation"/>.
    /// </summary>
    /// <remarks>
    /// [and execution time. (eventually)]
    /// </remarks>
    public class Fiber : Media.Common.SuppressedFinalizerDisposable, Media.Common.IUpdateable, Media.Common.IThreadReference
    {
        System.Threading.Thread UnderlyingThread;

        System.Threading.CompressedStack UnderlyingCompressedStack;

        ThreadPriorityInformation UnderlyingThreadsPriorityInformation;

        public Fiber(bool shouldDispose = true)
            : base(shouldDispose)
        {
            ManualResetEvent = new System.Threading.ManualResetEventSlim();

            UpdateTokenSource = new System.Threading.CancellationTokenSource();
        }

        public Fiber(System.Func<System.Exception, bool> exceptionHandler, bool shouldDispose = true)
            :base(shouldDispose)
        {
            ExceptionHandler = exceptionHandler;
        }

        // others such as

        //int Capacity;

        //--

        readonly System.DateTimeOffset Created = System.DateTimeOffset.UtcNow;

        public System.DateTimeOffset Started { get; protected set; }

        readonly static System.DateTimeOffset DefaultDateTimeOffset = default(System.DateTimeOffset);

        public bool IsStarted { get { return Started.Equals(DefaultDateTimeOffset).Equals(false); } }

        public bool HasUnderlyingCompressedStack { get { return object.ReferenceEquals(UnderlyingCompressedStack, null).Equals(false); } }

        //-- IUpdateable

        readonly System.Threading.ManualResetEventSlim ManualResetEvent;

        readonly System.Threading.CancellationTokenSource UpdateTokenSource;

        //-- The fibers or actions

        public readonly Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim <System.Tuple<System.Action, ThreadPriorityInformation, System.TimeSpan>> Itinerarius =
            new Media.Common.Collections.Generic.ConcurrentLinkedQueueSlim<System.Tuple<System.Action, ThreadPriorityInformation, System.TimeSpan>>();

        public bool Add(System.Action action)
        {
            if (object.ReferenceEquals(action, null)) return false;

            System.Tuple<System.Action, ThreadPriorityInformation, System.TimeSpan> Item = new System.Tuple<System.Action, ThreadPriorityInformation, System.TimeSpan>(action, UnderlyingThreadsPriorityInformation, System.Threading.Timeout.InfiniteTimeSpan);

            while (Itinerarius.TryEnqueue(ref Item).Equals(false) &&
                UpdateTokenSource.IsCancellationRequested.Equals(false))
            {
                while (System.Threading.Thread.Yield()) System.Threading.Thread.CurrentThread.Join();
            }

            return true;
        }

        public readonly EnumerableException Exceptions = new EnumerableException();

        static readonly System.Type StructureType = typeof(System.Tuple<System.Action, ThreadPriorityInformation, System.TimeSpan>);

        /// <summary>
        /// Normalize the itinerary
        /// </summary>
        void UniversalEntryPoint()
        {            
            goto Started;
        Started: Started = System.DateTimeOffset.UtcNow; goto Aft;
        Aft:
            System.Tuple<System.Action, ThreadPriorityInformation, System.TimeSpan> Item;
        Begin:
        try
        {
            try
            {
                while (Common.IDisposedExtensions.IsNullOrDisposed(this).Equals(false) &&
                    Itinerarius.Count > 0 && 
                    UpdateTokenSource.IsCancellationRequested.Equals(false))
                {
                    if (Itinerarius.TryDequeue(out Item))
                    {
                        UnderlyingThread.Priority = System.Threading.ThreadPriority.Lowest;

                        //Todo, execution time will require Timer or Clock.

                        try { Item.Item1.Invoke(); }
                        catch { throw; }
                        finally { UnderlyingThread.Priority = System.Threading.ThreadPriority.Highest; }
                    }
                }

                //Depleted
                UnderlyingThreadsPriorityInformation.SetIdle(UnderlyingThread);
            }
            catch (System.Exception)
            {
                UnderlyingThread.Priority = System.Threading.ThreadPriority.Normal;

                if (object.ReferenceEquals(ExceptionHandler, null).Equals(false))
                {
                    UnderlyingCompressedStack = System.Threading.CompressedStack.Capture();

                    using (var enumerator = Exceptions.GetExceptions())
                    {
                        while (enumerator.MoveNext() && UpdateTokenSource.IsCancellationRequested.Equals(false))
                        {
                            UnderlyingThread.Priority = System.Threading.ThreadPriority.BelowNormal; 

                            throw enumerator.Current;
                        }
                    }
                }

                goto Begin;
            }
        }
        catch(System.Exception ex)
        {
            UnderlyingThread.Priority = System.Threading.ThreadPriority.BelowNormal; 

            if (Exceptions.Handle(ex).Equals(false))
            {
                UnderlyingThread.Priority = System.Threading.ThreadPriority.Normal; 

                throw ex;
            }

            UnderlyingThread.Priority = System.Threading.ThreadPriority.AboveNormal;

            UnderlyingCompressedStack = null;
        }
        //finally
        //{
        //    unsafe { System.Runtime.InteropServices.Marshal.DestroyStructure(Unsafe.AddressOf(ref Item), StructureType); }
        //}
            
            //Wait for more work.
            if (object.ReferenceEquals(System.Threading.Thread.CurrentThread, UnderlyingThread))
            {
                System.Threading.Thread.Sleep(System.Threading.Timeout.InfiniteTimeSpan);

                goto Begin;
            }
        }

        #region Async

        readonly System.AggregateException AggregateExceptions = new System.AggregateException();

        readonly System.Func<System.Exception, bool> ExceptionHandler;

        async void AsyncEntryPoint()
        {
            goto Started;
                        Started: 
            Started = System.DateTimeOffset.UtcNow; 
                        goto Aft;
                Aft:
        Begin:

            System.Tuple<System.Action, ThreadPriorityInformation, System.TimeSpan> Item;
            try
            {
                while (Itinerarius.Count > 0 &&
                    UpdateTokenSource.IsCancellationRequested.Equals(false))
                {
                    if (Itinerarius.TryDequeue(out Item))
                    {
                        UpdateTokenSource.CancelAfter(Item.Item3);

                        UnderlyingThread.Priority = System.Threading.ThreadPriority.Lowest;

                        await System.Threading.Tasks.Task.Run(Item.Item1, UpdateTokenSource.Token);

                        UnderlyingThread.Priority = System.Threading.ThreadPriority.BelowNormal;
                    }
                    else
                    {
                        UnderlyingThread.Priority = System.Threading.ThreadPriority.BelowNormal;
                    }

                    if (object.ReferenceEquals(System.Threading.Thread.CurrentThread, UnderlyingThread))
                    {
                        UnderlyingThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(System.Threading.Timeout.InfiniteTimeSpan);
                    }
                }
            }
            catch (System.Exception)
            {
                if (object.ReferenceEquals(ExceptionHandler, null).Equals(false))
                {
                    AggregateExceptions.Handle(ExceptionHandler);

                    goto Begin;
                }
            }
            finally
            {
                if (object.ReferenceEquals(ExceptionHandler, null).Equals(false) &&
                    AggregateExceptions.Data.Count > 0) throw AggregateExceptions;
            }
            
            if (object.ReferenceEquals(System.Threading.Thread.CurrentThread, UnderlyingThread))
            {
                System.Threading.Thread.Sleep(System.Threading.Timeout.InfiniteTimeSpan);

                goto Begin;
            }
        }

        #endregion

        static void ConfigureFiber(Fiber fiber)
        {
            if (object.ReferenceEquals(fiber, null) || fiber.IsStarted || Common.IDisposedExtensions.IsNullOrDisposed(fiber)) return;

            fiber.UnderlyingThread.TrySetApartmentState(System.Threading.ApartmentState.MTA);

            fiber.UnderlyingThreadsPriorityInformation.AbortPriority = fiber.UnderlyingThreadsPriorityInformation.StartPriority = ThreadPriority.AboveNormal;

            fiber.UnderlyingThreadsPriorityInformation.IdlePriority = ThreadPriority.BelowNormal;

            fiber.UnderlyingThreadsPriorityInformation.RunningPriority = ThreadPriority.Highest;
        }

        public void Start()
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(this) || IsStarted) return;

            UnderlyingThread = new System.Threading.Thread(UniversalEntryPoint);

            UnderlyingThreadsPriorityInformation = new ThreadPriorityInformation();

            if (object.ReferenceEquals(ConfigureThread, null).Equals(false))
            {
                ConfigureThread(UnderlyingThread);
            }
            else
            {
                ConfigureFiber(this);
            }

            UnderlyingThreadsPriorityInformation.Start(UnderlyingThread);
        }

        public void Stop()
        {
            Dispose();
        }

        #region `exemplum`

        //bool Common.IDisposed.IsDisposed
        //{
        //    get { return this.IsDisposed; }
        //}

        //bool Common.IDisposed.ShouldDispose
        //{
        //    get { return this.ShouldDispose; }
        //}

        //void System.IDisposable.Dispose()
        //{
        //    this.Dispose();
        //}

        #endregion

        System.Threading.ManualResetEventSlim Common.IUpdateable.ManualResetEvent
        {
            get { return ManualResetEvent; }
        }

        System.Threading.CancellationTokenSource Common.IUpdateable.UpdateTokenSource
        {
            get { return UpdateTokenSource; }
        }

        System.Collections.Generic.IEnumerable<System.Threading.Thread> Common.IThreadReference.GetReferencedThreads()
        {
            return Common.IDisposedExtensions.IsNullOrDisposed(this) ? null : Media.Common.Extensions.Linq.LinqExtensions.Yield(UnderlyingThread);
        }

        public System.Action<System.Threading.Thread> ConfigureThread { get; set; }
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
        /// List of delegates to be called, we need it because it is relatevely easy to implement a loop with list modification inside of it
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
