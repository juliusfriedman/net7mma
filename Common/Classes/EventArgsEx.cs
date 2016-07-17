using System;
using System.Collections.Generic;

namespace Media.Common
{
    #region EventHandlerEx

    //Only used by ITransactionResult which should be removed eventually.

    public delegate void EventHandlerEx<in T>(object sender, T t);

    public delegate void EventHandlerEx(object sender, EventArgsEx t);

    #endregion

    //Ugly and Never will be used
    #region EventArgsEx

    public class EventArgsEx : EventArgs
    {
        public bool Handled { get; protected set; }

        public bool Continue { get; protected set; }

        public DateTimeOffset LastHandled { get; protected set; }

        public readonly HashSet<object> Ignore = new HashSet<object>() { null };

        public bool IgnoreSender(object o) { return Ignore.Add(o); }

        public bool IgnoresSender(object o){ return Ignore.Contains(o);}

        public bool ListenForSender(object o) { return Ignore.Remove(o); }

        public static void SetHandled(object sender, EventArgsEx args, bool value = true)
        {
            if(args != null && false == args.IgnoresSender(sender)) args.Handled = value;
        }

        public static void SetContinue(object sender, EventArgsEx args, bool value = true)
        {
            if (args != null && false == args.IgnoresSender(sender)) args.Continue = value;
        }

        public static void SetLastHandled(object sender, EventArgsEx args, DateTimeOffset? value = null)
        {
            if (args != null && false == args.IgnoresSender(sender)) args.LastHandled = value ?? DateTimeOffset.UtcNow;
        }
    }

    #endregion
    
    #region IEvent

    /// <summary>
    /// An interface which defined a <see cref="Delegate"/> which can be turned on or off.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Gets a value indicating if <see cref="Source"/> be invoked
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets a value which indicates the <see cref="Delegate"/> which will be invoked based on the value of <see cref="Enabled"/>
        /// </summary>
        Delegate Source { get; }

        /// <summary>
        /// Gets the value which possibly indicates the arguments and the return type of <see cref="Source"/>.
        /// </summary>
        /// <remarks>
        /// 
        /////// The structure of this value was highly dependant on the following:
        /////// Signature == 0 : No return value
        /////// Signature &lt;0 : Will throw an exception (-0)
        /////// Signature &gt;0 : May throw an exception
        /// </remarks>
        long Signature { get; }        
    }

    public static class IEventExtensions
    {
        public static bool HasSource(IEvent @event)
        {
            return Object.ReferenceEquals(@event, null).Equals(false) &&
                Object.ReferenceEquals(@event.Source, null).Equals(false);
        }

        public static bool IsEnabled(IEvent @event)
        {
            return Object.ReferenceEquals(@event, null).Equals(false) && @event.Enabled;
        }

        public static bool IsNull(IEvent @event)
        {
            return object.ReferenceEquals(@event, null);
        }
    }

    //--------------------------------------------------------------------------------------------------

    //IApplicationEvent : IEvent => Object ApplicationContext
    //IApplicationEvent<T> : I ApplicationEvent => T ApplicationElement

    //--------------------------------------------------------------------------------------------------

    internal class Eventbase : IEvent
    {
        public Eventbase(Delegate @event, bool enabled = true)
        {
            if (object.ReferenceEquals(@event, null)) throw new ArgumentNullException("@event");

            Source = @event;

            Enabled = enabled;
        }

        protected Eventbase(object o, bool enabled = true)
        {
            if (o is System.Delegate) Source = o as System.Delegate;

            Enabled = enabled;
        }        

        public Delegate Source { get; protected set; }

        public bool Enabled { get; protected set; }

        public long Signature { get; private set; }

        //While its possible to improve the way the call is made for performance, it's impossible to use a signature here unless dynamic utilized which required Binder.

        //void DynamicRaise(bool force, dynamic args)
        //{
        //    if (Enabled || force)
        //    {
        //        //Performance...
        //        Event.DynamicInvoke(args);
        //    }
        //}

        void Raise(bool force, params object[] args)
        {
            if (Enabled || force)
            {
                //Performance...
                Source.DynamicInvoke(args);
            }
        }

        void Disable() { Enabled = false; }

        void Enable() { Enabled = true; }

        void Toggle() { if (Enabled) Disable(); else Enable(); }
    }

    internal class Consumable<T> : Eventbase
    {
        public T Consumed { get; internal protected set; }

        public Consumable(T t, bool enabled)
            : base(t, enabled)
        {

        }       
    }

    internal struct EventReferenceInformation<T> : IEvent, IDisposed
    {
        internal long Signature;

        /// <summary>
        /// The <see cref="event"/> which is in reference which is also a <see cref="System.Delegate"/> to the logic which will be executed,
        /// This value may be null.
        /// </summary>
        /// <remarks>
        /// This is a reference type.
        /// </remarks>
        internal Delegate Event;

        /// <summary>
        /// Gets a value which is conventionally used as whatever is consumed by <see cref="Event"/> i.e. the arguments.
        /// If the <see cref="Event"/> has no arguments this may be the return type or some other value.
        /// </summary>
        internal T Consumed;

        /// <summary>
        /// Indicates if the instance should dispose
        /// </summary>
        internal bool ShouldDispose;

        public EventReferenceInformation(Delegate @event, ref long signature, bool shouldDispose = true)
        {
            ShouldDispose = shouldDispose;

            Consumed = default(T);

            Event = @event;

            Signature = signature;
        }

        public EventReferenceInformation(T t, Delegate @event, ref long signature, bool shouldDispose = true)
        {
            ShouldDispose = shouldDispose;

            Event = (Consumed = t) as Delegate;

            Signature = signature;
        }

        System.Collections.IList GetInvocationList()
        {
            return Event.GetInvocationList();
        }

        void Fire(System.Reflection.ParameterInfo[] optional = null)
        {
            Common.EventReferenceInformationExtensions.Consume(this, Consumed, optional);
        }

        bool IEvent.Enabled
        {
            get { return Common.IDisposedExtensions.IsNullOrDisposed(this).Equals(false) && IEventExtensions.IsEnabled(this); }
        }

        Delegate IEvent.Source
        {
            get
            {
                return Common.IDisposedExtensions.IsNullOrDisposed(this).Equals(false) ? 
                    IEventExtensions.HasSource(this) ? 
                        Event : Consumed as Delegate 
                            : null; 
            }
        }

        long IEvent.Signature
        {
            get { return Signature; }
        }

        bool IDisposed.IsDisposed
        {
            get { return this.IsNullOrDisposed(); }
        }

        bool IDisposed.ShouldDispose
        {
            get { return this.ShouldDispose; }
        }

        void IDisposable.Dispose()
        {
            if (this.IsNullOrDisposed() || // ....
                (ShouldDispose = IEventExtensions.IsNull(this).Equals(false))) return;

            this.Event = null;

            ShouldDispose = false;
        }
    }   

    internal static class EventReferenceInformationExtensions
    {
        internal static System.Delegate CreateDelegate(this EventReferenceInformation<System.Delegate> toFire, Type delegateType = null)
        {
            return toFire.Event.Method.CreateDelegate(delegateType ?? Media.Common.Extensions.Delegate.DelegateExtensions.TypeOfDelegate);
        }

        internal static System.Delegate CreateDelegate<T>(this IEvent @event, T t)
        {
            return System.Delegate.CreateDelegate(typeof(T), @event.Source.Method);
        }

        internal static System.Delegate CreateDelegate<T>(this IEvent @event, Type type)
        {
            return System.Delegate.CreateDelegate(type, @event.Source.Method);
        }

        internal static void Consume<T>(this IEvent @event, T t, System.Reflection.ParameterInfo[] parameterInfo = null)
        {
            if (Common.IEventExtensions.IsNull(@event)) return;

            System.Reflection.MethodInfo mi = @event.Source.Method;

            System.Reflection.ParameterInfo[] methodParams = parameterInfo ?? mi.GetParameters();

            System.Delegate @delegate = System.Delegate.CreateDelegate(typeof(T), mi);

            @delegate.DynamicInvoke();
        }
    }

#endregion

}
