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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Common;

#endregion

namespace Media.Common
{
    /// <summary>
    /// Provides an interface to allow access to the <see cref="Exception"/> class.
    /// </summary>
    public interface IExceptionEx
    {
        /// <summary>
        /// <see cref="Exception.InnerException"/>.
        /// </summary>
        Exception InnerException { get; }

        /// <summary>
        /// The <see cref="System.Object"/> which corresponds to the underlying exception.
        /// </summary>
        Object Tag { get; }
    }

    /// <summary>
    /// In short,
    /// All exceptions can be recovered from, only some can be resumed. 
    /// This class provides [a scope when used in conjunction with the `using` construct] methods to share data through as well as pass messages during programming if required.
    ///
    /// Define a real class, e.g. myException : Exception<paramref name="T"/> to only mangle the typename once if possible.
    /// 
    /// This class provides a base class which is derived from <see cref="System.Exception"/>.
    ///
    /// It allows a construct of programming based on scopes and exceptions.
    /// 
    /// It is not marked abstract because it would be useless.
    /// </summary>
    /// <typeparam name="T">The type data in the Tag property</typeparam>
    public class Exception<T> : Exception, IExceptionEx, IDisposable, System.Runtime.Serialization.ISerializable
    {
        /// <summary>
        /// The string which will be used on all instances if no message was provided when instantiated.
        /// </summary>
        public const string ExceptionFormat = "A System.Exception occured related to the following System.Type: `{0}`. If there is related data it is located in the Tag property.";

        public static string DefaultExceptionTypeMessage<T>() { return string.Format(Exception<T>.ExceptionFormat, typeof(T).FullName); }

        /// <summary>
        /// Check if we are in a exception unwind scenario or not.
        /// </summary>
        public static bool InException
        {
            get
            {   // Errata: The red marked code seems to be necessary. Since unit tests with .NET 2.0
                // have shown that only checking for the Exception Pointers structure does not always work.
                return ExceptionExtensions.InException;
            }
        }

        #region Fields

        bool m_Disposed;// = false;

        #endregion

        #region Properties

        /// <summary>
        /// The element which corresponds to the underlying exception
        /// </summary>
        public virtual T Tag { get; protected set; }

        /// <summary>
        /// <see cref="Exception.InnerException"/>.
        /// </summary>
        Exception IExceptionEx.InnerException
        {
            get { return base.InnerException; }
        }

        /// <summary>
        /// A boxed representation of the Tag property.
        /// </summary>
        object IExceptionEx.Tag
        {
            get { return this.Tag; }
        }

        /// <summary>
        /// Indicates if the Exception has been previously disposed
        /// </summary>
        public bool Disposed { get { return m_Disposed; } protected set { m_Disposed = value; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> in the <see cref="Tag"/> property.
        /// </summary>
        public Exception()
            : base() { Tag = default(T); }

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns <see cref="Exception.Message"/>
        /// </summary>
        /// <param name="tag">The value to store.</param>
        /// <param name="message">The message realted to the exception</param>
        public Exception(T tag, string message)
            : base(message) { Tag = tag; }

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns <see cref="Exception.Message"/> and <see cref="Exception.InnerException"/>
        /// </summary>
        /// <param name="tag">The value to store.</param>
        /// <param name="message">The message realted to the exception</param>
        /// <param name="innerException">The exception which superceeds this exception</param>
        public Exception(T tag, string message, Exception innerException)
            : base(message, innerException) { Tag = tag; }


        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns a default message describing the <typeparamref name="T"/>.
        /// </summary>
        /// <param name="tag">The value to store.</param>
        public Exception(T tag) : this(tag, DefaultExceptionTypeMessage<T>(), null) { }

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns <see cref="Exception.Message"/> and <see cref="Exception.InnerException"/> and optionally assigns any given Data.
        /// </summary>
        /// <param name="tag">The element related to the exception</param>
        /// <param name="message">The message realted to the exception, if not provided a default message will be used.</param>
        /// <param name="innerException">any <see cref="System.Exception"/> which is related to the exception being thrown</param>
        /// <param name="data">Any data which should be also stored with the exception</param>
        public Exception(T tag, string message, Exception innerException, params object[] data)
            : this(tag, message, innerException)
        {
            //If given any data 
            //Add any data related to the the Data Dictionary of the Exception using the Hashcode of the data as the key.
            if (data != null) foreach (object key in data) Data.Add(key.GetHashCode(), key);
        }

        /// <summary>
        /// Finalizes the instace by calling Dispose.
        /// </summary>
        ~Exception() { Dispose(); }

        #endregion

        #region Methods

        /// <summary>
        /// Disposes the exception
        /// </summary>
        public virtual void Dispose()
        {
            if (m_Disposed) return;
            GC.SuppressFinalize(this);
            m_Disposed = true;            
            //ClearData();
        }

        internal protected void ClearData() { Data.Clear(); }

        internal protected void AddData(object key, object value) { Data.Add(key, value); }

        #endregion
    }

    public static class ExceptionExtensions
    {       

         /// <summary>
        /// Check if we are in a exception unwind scenario or not.
        /// </summary>
        public static bool InException
        {
            get
            {   // Errata: The red marked code seems to be necessary. Since unit tests with .NET 2.0
                // have shown that only checking for the Exception Pointers structure does not always work.
                return System.Runtime.InteropServices.Marshal.GetExceptionPointers() == IntPtr.Zero && System.Runtime.InteropServices.Marshal.GetExceptionCode() == 0 ? false : true;
            }
        }

        public static void Raise<T>(this Exception<T> exception) { throw exception; }

        //Resumeable?

        /// <summary>
        /// Raises an <see cref="Common.Exception"/> on the calling thread.
        /// </summary>
        /// <typeparam name="T">The type of the exception to raise.</typeparam>
        /// <param name="tag">The element related to the exception</param>
        /// <param name="message">The message realted to the exception, if not provided a default message will be used.</param>
        /// <param name="innerException">any <see cref="System.Exception"/> which is related to the exception being thrown</param>
        public static void CreateAndRaiseException<T>(T tag, string message, Exception innerException = null) { new Exception<T>(tag, message ?? Exception<T>.DefaultExceptionTypeMessage<T>(), innerException).Raise(); }
    }

}
