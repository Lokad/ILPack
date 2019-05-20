ildasm /bytes bin\Debug\netcoreapp2.1\TestSubject.dll > il.original.txt
ildasm /bytes bin\Debug\netcoreapp2.1\cloned\ClonedTestSubject.dll > il.clone.txt
mddumper bin\Debug\netcoreapp2.1\TestSubject.dll > md.original.txt
mddumper bin\Debug\netcoreapp2.1\cloned\ClonedTestSubject.dll > md.clone.txt
