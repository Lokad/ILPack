ildasm ./bin/Debug/netcoreapp2.1/RewriteOriginal.dll > il.original.txt
ildasm ./bin/Debug/netcoreapp2.1/cloned/RewriteOriginal.dll > il.cloned.txt
mddumper ./bin/Debug/netcoreapp2.1/RewriteOriginal.dll > md.original.txt
mddumper ./bin/Debug/netcoreapp2.1/cloned/RewriteOriginal.dll > md.cloned.txt

