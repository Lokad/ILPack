# Signing Lokad.ILPack

In .NET 5+, strong naming is not about security or authority: it's only about identity. Thus, the `Lokad.ILPack.snk` gets included into the repository.

More information:
<https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/strong-naming>

## Checking that a DLL is strong-named

Use the following function against the `.dll` file of interest:

```powershell
function Get-AssemblyStrongName($assemblyPath)
{
    $fullpath = Convert-Path $assemblyPath
    [System.Reflection.AssemblyName]::GetAssemblyName($fullpath).FullName
}```
