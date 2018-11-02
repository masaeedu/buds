using System.IO;
using System.Linq;

using AssemblySerialization;

using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        string _assemblyFile;

        [SetUp]
        public void Setup()
        {
            _assemblyFile = Path.GetTempFileName();

            using (var source = File.OpenRead(@"D:\depot\git\buds\src\Buds\bin\Debug\netstandard1.5\Buds.dll"))
            using (var dest = File.OpenWrite(_assemblyFile))
            {
                source.CopyTo(dest);
            }
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(_assemblyFile);
        }

        [Test]
        public void RoundTripSerialization()
        {
            var data1 = File.ReadAllBytes(_assemblyFile);
            var data2 = new[] { data1 }.DeserializeAndLoadAssemblyGraph().Single().Serialize().Single();

            Assert.True(data1.SequenceEqual(data2));
        }
    }
}