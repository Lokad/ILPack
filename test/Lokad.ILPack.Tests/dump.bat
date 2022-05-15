ildasm /bytes bin\Debug\net6.0\TestSubject.dll > il.original.txt
ildasm /bytes bin\Debug\net6.0\cloned\ClonedTestSubject.dll > il.clone.txt
mddumper bin\Debug\net6.0\TestSubject.dll > md.original.txt
mddumper bin\Debug\net6.0\cloned\ClonedTestSubject.dll > md.clone.txt
