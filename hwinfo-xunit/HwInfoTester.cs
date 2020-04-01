using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using PromDapterDeclarations;
using SensorMonHTTP;
using SharpYaml;
using SharpYaml.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace hwinfo_xunit
{
    public class HwInfoTester
    {
        public HwInfoTester(ITestOutputHelper outputHelper)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(
                message => outputHelper.WriteLine(message));
        }

        [Fact]
        public async Task TestYamlSerialization()
        {
            //var yamlContent = await File.ReadAllTextAsync();
            const string fileName = "Prometheusmapping.yaml";
            object obj = null;
            using (var textStream = File.OpenText(fileName))
            {
                var serializer = new Serializer(new SerializerSettings());
                obj = serializer.Deserialize<ExpandoObject>(textStream);
            }

            dynamic dyn = obj;
            foreach (var mapping in dyn.mapping)
            {
                string name = mapping["name"];
                string[] patterns = ((List<object>) mapping["patterns"]).Cast<string>().ToArray();
                Debug.WriteLine($"Name: {name}");
                Debug.WriteLine($"Pattern(s):");
                Debug.WriteLine(String.Join(Environment.NewLine, patterns));
            }
        }

        [Fact]
        public async Task TestHWInfoProvider()
        {
            IPromDapterService service = new HWiNFOProvider();
            DataItem[] data = null;
            await service.Open();
            for (int i = 0; i < 10000; i++)
            {
                data = await service.GetDataItems();
            }
            await service.Close(true);
            Assert.Equal("Virtual Memory Commited", data.First().Name);
            Assert.Equal("MB", data.First().Unit);
        }

        private void assertMemoryState()
        {
            //expectAmount(typeof(HWiNFOProvider._HWiNFO_SENSORS_SHARED_MEM2), 0);
            //expectAmount(typeof(HWiNFOProvider._HWiNFO_SENSORS_READING_ELEMENT), 0);
            //expectAmount(typeof(HWiNFOProvider._HWiNFO_SENSORS_SENSOR_ELEMENT), 0);
            expectAmount(typeof(MemoryMappedViewAccessor), 0);
            expectAmount(typeof(GCHandle), 0);
            expectAmount(typeof(HWiNFOProvider), 1);
            void expectAmount(Type type, int expected = 0)
            {
                dotMemory.Check(memory =>
                    Assert.Equal(expected, memory.GetObjects(item => item.Type.Is(type)).ObjectsCount));
            }
        }

        [Fact]
        public async Task TestHWInfoProviderMemoryLeaks()
        {
            IPromDapterService service = new HWiNFOProvider();

            for (int i = 0; i < 10000; i++)
            {
                await service.Open();
                var data = await service.GetDataItems();
                await service.Close(true);
            }

            assertMemoryState();

            /*
            dotMemory.Check(memory =>
                Assert.Equal(0,
                    memory.GetObjects(where => where.Type.Is<HWiNFOProvider._HWiNFO_SENSORS_SHARED_MEM2>())
                        .ObjectsCount));
            dotMemory.Check(memory =>
                Assert.Equal(0,
                    memory.GetObjects(where => where.Type.Is<HWiNFOProvider._HWiNFO_SENSORS_SENSOR_ELEMENT>())
                        .ObjectsCount));
            dotMemory.Check(memory =>
                Assert.Equal(0,
                    memory.GetObjects(where => where.Type.Is<HWiNFOProvider._HWiNFO_SENSORS_READING_ELEMENT>())
                        .ObjectsCount));
            */
        }

        [Fact]
        public void TestHWInfo()
        {
            var wrapper = new HWiNFOWrapper();
            List<string> textLines = new List<string>();
            HWiNFOWrapper.Console.WriteLine = line => textLines.Add(line);
            HWiNFOWrapper.Console.ReadLine = () =>
            {
                int i = 0;
            };
            for (int i = 0; i < 10000; i++)
            {
                wrapper.Open(false);
                wrapper.Close(false);
                //wrapper.Close(false);
            }
            wrapper.Close(true);
            //var content = String.Join(Environment.NewLine, textLines);
        }
    }
}
