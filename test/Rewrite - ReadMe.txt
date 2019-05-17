## Rewrite Testing

These projects are intended to help test and debug ILPack:

* RewriteOriginal - an assembly with a set of types to be tested for ILPacking
* RewriteTool - a simple command line tool that takes one assembly and rewrites it as another
* RewriteClient - client program of RewriteOriginal that invokes the rewritten assembly

## Notes

* To test against the original assembly instead of the rewritten one, just edit the RewriteClient
  .csproj file and add/remove the "/cloned" part of the reference path

* On building RewriteOriginal, the RewriteTool is invoked to automatically rewrite the output 
  assembly to a /cloned subdirectory of the output directory.  If you change ILPack, you'll
  need to rebuild RewriteOriginal to get a new rewritten file in the /cloned folder.

* In the RewriteOriginal project folder is a batch file "dump.bat" that will invoke ildasm
  and mddumper to list out the contents of both the original assembly and the rewritten cloned
  assembly for comparing to help diagnose problems.  Both ildasm and mddumper need to be on 
  your path for this to work.


## Third Party Tools

* mddumper 
    - https://github.com/Microsoft/dotnet-samples/tree/master/System.Reflection.Metadata/MdDumper/MdDumper

* ildasm	
	- https://www.nuget.org/packages?q=ildasm
    - https://www.nuget.org/packages/Microsoft.NETCore.ILDAsm/
	- https://www.nuget.org/packages/runtime.win-x64.Microsoft.NETCore.ILDAsm/

