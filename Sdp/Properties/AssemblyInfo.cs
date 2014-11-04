using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Media")]
[assembly: AssemblyDescription("Provides a managed implementation of RFC4566")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("v//")]
[assembly: AssemblyProduct("Media")]
[assembly: AssemblyCopyright("Copyright © Julius R. Friedman")]
[assembly: AssemblyTrademark("v//")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b9788cdc-7365-4117-9711-2cfa2057e41e")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.7.*")]
[assembly: AssemblyFileVersion("1.0.7.*")]
[assembly: System.CLSCompliant(true)]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Media.Rtp")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Media.Rtsp")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Media.RtspServer")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Tests")]