﻿/*
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

namespace Media.Common.Interfaces
{
    /// <summary>
    /// Represents an interface which can indicate if the instance has any ability to be written to
    /// </summary>
    public interface IReadOnly
    {
        /// <summary>
        /// Indicates if the instance currently has any ability to be executed or modified.
        /// </summary>
        bool IsReadOnly { get; } //=> false
    }

    /// <summary>
    /// Represents an interface which can indicate if the instance has any ability to be written to
    /// </summary>
    public interface IWriteOnly
    {
        /// <summary>
        /// Indicates if the instance currently has any abiity to be written to.
        /// </summary>
        bool IsWriteOnly { get; } //=> this is IReadOnly ? false : true
    }

    /// <summary>
    /// Represents a combination of the <see cref="IReadOnly"/> and <see cref="IWriteOnly"/> interfaces which can also indicate if the instance has the ability to change value.
    /// </summary>
    public interface IMutable : IReadOnly, IWriteOnly
    {
        /// <summary>
        /// Gets a value which indicates if the instance can change value.
        /// </summary>
        /// <remarks>
        /// When <see cref="true"/> implies that <see cref="IsReadOnly"/> or <see cref="IsWriteOnly"/> can change value.
        /// </remarks>
        bool Mutable { get; }
    }

    //See ITries.cs which contains closely related paradigms.
}
