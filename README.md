# Lokad.ILPack

Exports a .NET type to a serialized assembly, with support for dynamic
assemblies (i.e. custom IL generation). This library is intended as a
drop-in replacement for the `AssemblyBuilder.Save` method which existed 
since .NET 1.1 but that has not been ported to .NET Core 3.0.

To install with NuGet:

    Install-Package Lokad.ILPack

Usage:

```cs
var assembly = Assembly.GetAssembly(t);
var generator = new Lokad.ILPack.AssemblyGenerator();

// for ad-hoc serialization
var bytes = generator.GenerateAssemblyBytes(assembly);

// direct serialization to disk
generator.GenerateAssembly(assembly, "/path/to/file");
```

Released under the MIT license.
