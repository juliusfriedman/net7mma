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
    public class TaggedException<T> : Exception, ITaggedException, IDisposed
    {
        #region Statics

        /// <summary>
        /// The string which will be used on all instances if no message was provided when instantiated.
        /// </summary>
        public const string ExceptionFormat = "A System.Exception occured related to the following System.Type: `{0}`. If there is related data it is located in the Tag property.";

        public static string DefaultExceptionTypeMessage<t>() { return string.Format(TaggedException<t>.ExceptionFormat, typeof(T).FullName); }

        /// <see cref="ExceptionExtensions.InException"/>
        //public static bool InException { get { return Media.Common.Extensions.Exception.ExceptionExtensions.InException; } }

        #endregion

        #region Fields

        readonly Common.CommonDisposable @base = new Common.CommonDisposable(true);

        #endregion

        #region Properties

        /// <summary>
        /// The element which corresponds to the underlying exception
        /// </summary>
        public virtual T Tag { get; protected set; }

        /// <summary>
        /// <see cref="Exception.InnerException"/>.
        /// </summary>
        Exception ITaggedException.InnerException
        {
            get { return base.InnerException; }
        }

        /// <summary>
        /// A boxed representation of the Tag property.
        /// </summary>
        object ITaggedException.Tag
        {
            get { return this.Tag; }
        }

        /// <summary>
        /// Indicates if the Exception has been previously disposed
        /// </summary>
        public bool IsDisposed { get { return @base.IsDisposed; } }

        public bool ShouldDispose { get { return @base.ShouldDispose; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> in the <see cref="Tag"/> property.
        /// </summary>
        public TaggedException()
            : base() { Tag = default(T); }

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns <see cref="Exception.Message"/>
        /// </summary>
        /// <param name="tag">The value to store.</param>
        /// <param name="message">The message realted to the exception</param>
        public TaggedException(T tag, string message)
            : base(message) { Tag = tag; }

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns <see cref="Exception.Message"/> and <see cref="Exception.InnerException"/>
        /// </summary>
        /// <param name="tag">The value to store.</param>
        /// <param name="message">The message realted to the exception</param>
        /// <param name="innerException">The exception which superceeds this exception</param>
        public TaggedException(T tag, string message, Exception innerException)
            : base(message, innerException) { Tag = tag; }


        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns a default message describing the <typeparamref name="T"/>.
        /// </summary>
        /// <param name="tag">The value to store.</param>
        public TaggedException(T tag) : this(tag, DefaultExceptionTypeMessage<T>(), null) { }

        /// <summary>
        /// Creates the default for <typeparamref name="T"/> from <paramref name="tag"/> in <see cref="Tag"/> and assigns <see cref="Exception.Message"/> and <see cref="Exception.InnerException"/> and optionally assigns any given Data.
        /// </summary>
        /// <param name="tag">The element related to the exception</param>
        /// <param name="message">The message realted to the exception, if not provided a default message will be used.</param>
        /// <param name="innerException">any <see cref="System.Exception"/> which is related to the exception being thrown</param>
        /// <param name="data">Any data which should be also stored with the exception</param>
        public TaggedException(T tag, string message, Exception innerException, params object[] data)
            : this(tag, message, innerException)
        {
            //If given any data 
            //Add any data related to the the Data Generic.Dictionary of the Exception using the Hashcode of the data as the key.
            if (data != null) foreach (object key in data) Data.Add(key.GetHashCode(), key);
        }

        /// <summary>
        /// Finalizes the instace by calling Dispose.
        /// </summary>
        ~TaggedException() { Dispose(); }

        #endregion

        #region Methods

        /// <summary>
        /// Disposes the exception
        /// </summary>
        public virtual void Dispose()
        {
            if (@base.IsDisposed) return;

            GC.SuppressFinalize(this);

            @base.Dispose();

            //ClearData();
        }

        internal protected void ClearData() { Data.Clear(); }

        internal protected void AddData(object key, object value) { Data.Add(key, value); }

        #endregion
    }

    //TaggedExceptionExtensions...
}
