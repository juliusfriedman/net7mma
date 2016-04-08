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

namespace Media.Common.Extensions
{
    public static class RuntimeExtensions
    {
        internal static readonly System.Type MonoType = System.Type.GetType("Mono.Runtime");

        public static bool IsMono { get { return MonoType != null; } }

        internal static readonly System.Type MonoMacType = System.Type.GetType("MonoMac");

        public static bool IsMonoMac { get { return MonoMacType != null; } }

        //Maybe a IsMonoOrMonoMac...

        internal static readonly System.Type MonoTouchObjCRuntimeType = System.Type.GetType("MonoTouch.ObjCRuntime");
        
        public static bool IsiOS { get { return MonoTouchObjCRuntimeType != null; } }

        //To have a Classic property one would need to ensure MonoTouchObjCRuntimeType != null and reflect the Constants.Version field.
        /*
         https://developer.xamarin.com/recipes/ios/general/projects/environment_checks/
         * 
         Version version = new Version (ObjCRuntime.Constants.Version);
         if (version > new Version (7,0))
         {
           // Code that uses features from Xamarin.iOS 7.0
         }
         */

        internal static readonly System.Type AndroidOSType = System.Type.GetType("Android.OS");

        public static bool IsAndroid { get { return AndroidOSType != null; } }

        internal static readonly System.Type WatchKitType = System.Type.GetType("WatchKit");

        /// <summary>
        /// Indicates if the WatchKit namespace is available.
        /// </summary>
        public static bool IsWatchKit { get { return WatchKitType != null; } }

        //IsRyuJit - http://stackoverflow.com/questions/22422021/how-do-i-verify-that-ryujit-is-jitting-my-app

        // Check whether optimized SIMD intrinsics will be generated
        //if (VectorMath.IsHardwareAccelerated)
        //{
        //}

        //http://stackoverflow.com/questions/8413922/programmatically-determining-mono-runtime-version

        //System.Reflection.Assembly.GetExecutingAssembly().ImageRuntimeVersion
        //NetVersion http://www.codeproject.com/Tips/135964/Get-NET-Framework-version

        ////https://github.com/dotnet/cli/blob/ac4c6702d5d345dc9c397f80290f105fedebe98b/scripts/Microsoft.DotNet.Cli.Build.Framework/CurrentPlatform.cs
    }
}
