﻿#region Copyright
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

#endregion

namespace Media.Common
{
    /// <summary>
    /// A <see cref="CommonDisposable"/> implementation which determines <see cref="ShouldDispose"/> based on the <see cref="HalfLife"/>
    /// </summary>
    public class LifetimeDisposable : CommonDisposable
    {
        #region Readonly Fields

        /// <summary>
        /// The date and time which this instance was created
        /// </summary>
        public readonly DateTimeOffset CreatedUtc = DateTimeOffset.UtcNow;

        #endregion

        #region Properties

        /// <summary>
        /// The amount of time this instance should stay alive
        /// </summary>
        public TimeSpan Lifetime { get; protected set; }

        /// <summary>
        /// The amount of time remaining since <see cref="CreatedUtc"/> before <see cref="LifetimeElapsed"/> is true.
        /// </summary>
        public TimeSpan HalfLife { get { return DateTimeOffset.UtcNow - CreatedUtc; } }

        public bool LifetimeElapsed { get { return HalfLife > Lifetime; } }

        #endregion

        #region Constructor

        public LifetimeDisposable(bool shouldDispose)
            : base(shouldDispose)
        {
            Lifetime = Common.Extensions.TimeSpan.TimeSpanExtensions.OneHour;
        }

        public LifetimeDisposable(bool shouldDispose, TimeSpan lifetime)
            : base(shouldDispose)
        {
            Lifetime = lifetime;
        }

        #endregion

        #region Methods

        protected void Expire() { Lifetime = HalfLife; }

        //Dispose could check for lifetime and then reschedule for finalize.

        internal protected override void Dispose(bool disposing)
        {
            //if (false == ShouldDispose && false == disposing && LifetimeElapsed) disposing = ShouldDispose = true;

            //base.Dispose(disposing);

            if (disposing) Expire();

            //Set disposing and ShouldDispose at the same time based on => (ShouldDispose || disposing || LifetimeElapsed)
            //Really only need to set ShouldDispose here..
            base.Dispose(disposing = ShouldDispose = ShouldDispose || disposing || LifetimeElapsed);
        }

        #endregion
    }
}
