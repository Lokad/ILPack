using Lokad.ILPack;
using System;
using System.Reflection;

namespace RewriteTool
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: RewriteTool sourceFile targetFile");
                return 7;
            }

            try
            {
                var fullPath = System.IO.Path.GetFullPath(args[0]);

                // Load the original assembly
                var originalAssembly = Assembly.LoadFile(fullPath);

                // Make sure output directory exists
                var outFile = System.IO.Path.GetFullPath(args[1]);
                var outDir = System.IO.Path.GetDirectoryName(outFile);
                System.IO.Directory.CreateDirectory(outDir);

                // Rewrite it...
                var generator = new AssemblyGenerator();
                generator.GenerateAssembly(originalAssembly, outFile);

                Console.WriteLine($"Rewrote: {outFile}");

                // Done!
                return 0;
            }
            catch (Exception x)
            {
                Console.WriteLine(x.StackTrace);
                Console.WriteLine($"Failed: {x.Message}");
                return 7;
            }
        }
    }
}
