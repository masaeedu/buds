using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace AssemblySerialization
{
    public static class AssemblyExtensions
    {
        public static IReadOnlyList<byte[]> Serialize(this Assembly assembly)
        {
            return assembly.GetReferencedAssemblies()
                .Where(an => false)
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyName)
                .Concat(new[]
                {
                    assembly
                })
                .Select(a => a.Location)
                .Where(File.Exists)
                .Select(File.ReadAllBytes)
                .ToList();
        }

        public static IReadOnlyList<Assembly> DeserializeAndLoadAssemblyGraph(this IEnumerable<byte[]> assemblyData)
        {
            return assemblyData.Select(d =>
            {
                using (var reader = new MemoryStream(d))
                {
                    return AssemblyLoadContext.Default.LoadFromStream(reader);
                }
            }).ToList();
        }
    }
}
