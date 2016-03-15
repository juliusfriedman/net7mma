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

    //Todo, Implement something more useful..

    ////IEvent...

    //    //Enabled, Event, Raise(bool force, object sender = null(this))


    //public interface IEvent
    //{
    //    bool Enabled { get; }

    //    Delegate Event { get; }
    //}

    //public class EnableableEvent :IEvent
    //{
    //    public EnableableEvent(Delegate @event, bool enabled = true)
    //    {
    //        if (@event == null) throw new ArgumentNullException("@event");

    //        Event = @event;

    //        Enabled = enabled;
    //    }

    //    public Delegate Event { get; protected set; }

    //    public bool Enabled { get; protected set; }

    //    //While its possible to improve the way the call is made for performance, it's impossible to use a signature here unless dynamic utilized which required Binder.

    //    void DynamicRaise(bool force, dynamic args)
    //    {
    //        if (Enabled || force)
    //        {
    //            //Performance...
    //            Event.DynamicInvoke(args);
    //        }
    //    }

    //    void Raise(bool force, params object[] args)
    //    {
    //        if (Enabled || force)
    //        {
    //            //Performance...
    //            Event.DynamicInvoke(args);
    //        }
    //    }

    //    void Disable() { Enabled = false; }

    //    void Enable() { Enabled = true; }
    //}

#endregion

}
