using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Orleans.TelemetryConsumers.MetricsTracker")]
[assembly: AssemblyDescription("An extension for Orleans which intercepts all telemetry calls, calculates and tracks the metrics on each silo, and aggregates all of the silo metrics into a ClusterMetricsGrain. The metrics stored in this grain can be accessed from any grain or Orleans client.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Orleans.TelemetryConsumers.MetricsTracker")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a3cc413e-e076-4f69-8d8a-93a4dee16566")]

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
[assembly: AssemblyVersion("0.9.6")]
[assembly: AssemblyFileVersion("0.9.6")]
[assembly: AssemblyInformationalVersion("0.9.6-alpha")]
