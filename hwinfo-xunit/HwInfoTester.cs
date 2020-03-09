using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PromDapterDeclarations;
using SensorMonHTTP;
using SharpYaml;
using SharpYaml.Serialization;
using Xunit;

namespace hwinfo_xunit
{
    public class HwInfoTester
    {
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
            await service.Open();
            DataItem[] data;
            for (int i = 0; i < 10000; i++)
            {
                data = await service.GetDataItems();
            }
            await service.Close(true);
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
                wrapper.Close(true);
                //wrapper.Close(false);
            }
            wrapper.Close(true);
            //var content = String.Join(Environment.NewLine, textLines);
        }
    }
}
