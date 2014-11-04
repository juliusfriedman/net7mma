using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Media.Rtp")]
[assembly: AssemblyDescription("Provides a mananged implementation of RFC3550")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b9788cdc-7365-4117-9711-2cfa2057e415")]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Media.Rtsp")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Media.RtspServer")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Media.Tests")]