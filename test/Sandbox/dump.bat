ildasm /bytes bin\Debug\netcoreapp2.1\SandboxSubject.dll > il.original.txt
ildasm /bytes bin\Debug\netcoreapp2.1\cloned\ClonedSandboxSubject.dll > il.clone.txt
mddumper bin\Debug\netcoreapp2.1\SandboxSubject.dll > md.original.txt
mddumper bin\Debug\netcoreapp2.1\cloned\ClonedSandboxSubject.dll > md.clone.txt
