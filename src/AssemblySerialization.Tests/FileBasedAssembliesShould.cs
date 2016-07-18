using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssemblySerialization.Tests
{
    public class SingleModuleAssembliesShould
    {

        public class WhenFileBased : IDisposable
        {
            readonly string _assemblyFile;

            public WhenFileBased()
            {
                _assemblyFile = Path.GetTempFileName();

                using (var source = File.OpenRead(@"D:\depot\git\buds\src\Buds\bin\Debug\netstandard1.5\Buds.dll"))
                using (var dest = File.OpenWrite(_assemblyFile))
                {
                    source.CopyTo(dest);
                }
            }

            [Fact]
            public void RoundTripSerialization()
            {
                var data1 = File.ReadAllBytes(_assemblyFile);
                var data2 = new [] { data1 }.DeserializeAndLoadAssemblyGraph().Single().Serialize().Single();

                Assert.True(data1.SequenceEqual(data2));
            }

            public void Dispose()
            {
                File.Delete(_assemblyFile);
            }
        }
    }
}
