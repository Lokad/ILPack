ildasm /bytes bin\Debug\net6.0\SandboxSubject.dll > il.original.txt
ildasm /bytes bin\Debug\net6.0\cloned\ClonedSandboxSubject.dll > il.clone.txt
mddumper bin\Debug\net6.0\SandboxSubject.dll > md.original.txt
mddumper bin\Debug\net6.0\cloned\ClonedSandboxSubject.dll > md.clone.txt
