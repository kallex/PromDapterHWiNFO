using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PromDapterDeclarations;

namespace SensorMonHTTP 
{
    public class HWiNFOProvider : IPromDapterService
    {
        const string HWiNFO_SENSORS_MAP_FILE_NAME2 = "Global\\HWiNFO_SENS_SM2";
        const string HWiNFO_SENSORS_SM2_MUTEX = "Global\\HWiNFO_SM2_MUTEX";
        const int HWiNFO_SENSORS_STRING_LEN2 = 128;
        const int HWiNFO_UNIT_STRING_LEN = 16;

        public HWiNFOProvider()
        {
            Close = CloseAsync;
            //GetDataItems = GetDataItemsBulk;
            GetDataItems = GetDataItemsSimple;
            Open = OpenAsync;
        }
        public Open Open { get; }
        public GetDataItems GetDataItems { get; }
        public Close Close { get; }


        private enum SENSOR_READING_TYPE
        {
            SENSOR_TYPE_NONE = 0,
            SENSOR_TYPE_TEMP,
            SENSOR_TYPE_VOLT,
            SENSOR_TYPE_FAN,
            SENSOR_TYPE_CURRENT,
            SENSOR_TYPE_POWER,
            SENSOR_TYPE_CLOCK,
            SENSOR_TYPE_USAGE,
            SENSOR_TYPE_OTHER
        };

        private static Dictionary<SENSOR_READING_TYPE, string> SensorTypeNameDictionary;

        static HWiNFOProvider()
        {
            var enumType = typeof(SENSOR_READING_TYPE);
            var enumValues = Enum.GetValues(enumType);
            SensorTypeNameDictionary = new Dictionary<SENSOR_READING_TYPE, string>()
            {
                { SENSOR_READING_TYPE.SENSOR_TYPE_NONE, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_NONE) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_TEMP, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_TEMP) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_VOLT, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_VOLT) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_FAN, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_NONE) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_CURRENT, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_CURRENT) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_POWER, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_POWER) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_CLOCK, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_CLOCK) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_USAGE, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_USAGE) },
                { SENSOR_READING_TYPE.SENSOR_TYPE_OTHER, nameof(SENSOR_READING_TYPE.SENSOR_TYPE_OTHER) },
            };
            /*
            foreach (var enumValue in enumValues)
            {
                var enumName = Enum.GetName(enumType, enumValue);
                SensorTypeNameDictionary.Add((SENSOR_READING_TYPE) enumValue, enumName);
            }*/
        }

  
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        private struct _HWiNFO_SENSORS_READING_ELEMENT
        {
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal SENSOR_READING_TYPE tReading;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwSensorIndex;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwReadingID;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            internal string szLabelOrig;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            internal string szLabelUser;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_UNIT_STRING_LEN)]
            internal string szUnit;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal double Value;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal double ValueMin;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal double ValueMax;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal double ValueAvg;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        private struct _HWiNFO_SENSORS_SENSOR_ELEMENT
        {
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwSensorID;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwSensorInst;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            internal string szSensorNameOrig;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            internal string szSensorNameUser;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        private struct _HWiNFO_SENSORS_SHARED_MEM2
        {
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwSignature;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwVersion;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwRevision;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal long poll_time;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwOffsetOfSensorSection;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwSizeOfSensorElement;
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwNumSensorElements;
            // descriptors for the Readings section
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwOffsetOfReadingSection; // Offset of the Reading section from beginning of HWiNFO_SENSORS_SHARED_MEM2
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwSizeOfReadingElement;   // Size of each Reading element = sizeof( HWiNFO_SENSORS_READING_ELEMENT )
            //[Obfuscation(Feature = "virtualization", Exclude = false)]
            internal UInt32 dwNumReadingElements;     // Number of Reading elements
        };
    

	    MemoryMappedFile mmf;

        private void CloseProvider(bool closeBuffers = false)
        {
            mmf?.Dispose();
        }

        private async Task OpenAsync(params object[] parameters)
        {
            mmf = MemoryMappedFile.OpenExisting(HWiNFO_SENSORS_MAP_FILE_NAME2, MemoryMappedFileRights.Read);
        }

        //[Obfuscation(Feature = "virtualization", Exclude = false)]
        private async Task<DataItem[]> GetDataItemsSimple(params object[] parameters)
        {

            using (var accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf(typeof(_HWiNFO_SENSORS_SHARED_MEM2)),
                MemoryMappedFileAccess.Read))
            {
                _HWiNFO_SENSORS_SHARED_MEM2 memInfo;
                accessor.Read(0, out memInfo);

                var numSensors = memInfo.dwNumSensorElements;
                var numReadingElements = memInfo.dwNumReadingElements;
                var offsetSensorSection = memInfo.dwOffsetOfSensorSection;
                var sizeSensorElement = memInfo.dwSizeOfSensorElement;
                var offsetReadingSection = memInfo.dwOffsetOfReadingSection;
                var sizeReadingElement = memInfo.dwSizeOfReadingElement;

                List<string> sensorNames = new List<string>();

                
                using (var sensor_element_accessor = mmf.CreateViewStream(
                    offsetSensorSection + 0, numSensors * sizeSensorElement,
                    MemoryMappedFileAccess.Read))
                {
                    var buffer = new byte[sizeSensorElement];
                    var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        for (UInt32 dwSensor = 0; dwSensor < numSensors; dwSensor++)
                        {
                            sensor_element_accessor.Read(buffer, 0, (int) sizeSensorElement);
                            var SensorElement = Marshal.PtrToStructure<_HWiNFO_SENSORS_SENSOR_ELEMENT>(handle.AddrOfPinnedObject());
                            sensorNames.Add(SensorElement.szSensorNameUser);
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }

                var sources = sensorNames.Select(item => new Source() {SourceName = item}).ToArray();
                var dataItems = new List<(Source source, string category, string name, string unit, Type valueType, object valueObject, DateTime timestamp)>();
                var timestamp = DateTime.UtcNow;

                using (var sensor_element_accessor = mmf.CreateViewStream(
                    offsetReadingSection, sizeReadingElement * numReadingElements, MemoryMappedFileAccess.Read))
                {
                    var buffer = new byte[sizeReadingElement];
                    var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        for (UInt32 dwReading = 0; dwReading < numReadingElements; dwReading++)
                        {
                            sensor_element_accessor.Read(buffer, 0, (int) sizeReadingElement);
                            var readingElement =
                                Marshal.PtrToStructure<_HWiNFO_SENSORS_READING_ELEMENT>(handle.AddrOfPinnedObject());

                            var currSource = sources[readingElement.dwSensorIndex];
                            /*
                            var dataItem = new DataItem()
                            {
                                Source = currSource,
                                //Category = readingElement.tReading.ToString(),
                                Category = SensorTypeNameDictionary[readingElement.tReading],
                                Name = readingElement.szLabelUser,
                                Unit = readingElement.szUnit,
                                Value = new DataValue()
                                {
                                    Type = typeof(double),
                                    Object = readingElement.Value
                                },
                                Timestamp = timestamp
                            };
                            */
                            var category = SensorTypeNameDictionary[readingElement.tReading];
                            var dataItem = (source: currSource, category: category, name: readingElement.szLabelUser,
                                unit: readingElement.szUnit, valueType: typeof(double),
                                valueObject: (object) readingElement.Value, timestamp: timestamp);
                            dataItems.Add(dataItem);
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
                var result = dataItems.Select(item => new DataItem()
                {
                    Source = item.source,
                    Category = item.category,
                    Name = item.name,
                    Unit = item.unit,
                    Value = new DataValue()
                    {
                        Type = item.valueType,
                        Object = item.valueObject
                    }
                }).ToArray();
                return result;
            }
        }

        private async Task CloseAsync(params object[] parameters)
        {
            var firstParam = parameters.FirstOrDefault();
            bool closeBuffers = false;
            if (firstParam != null && firstParam is bool)
            {
                closeBuffers = (bool) firstParam;
            }
            CloseProvider(closeBuffers);
        }

    };

}

// ***************************************************************************************************************
//                                          HWiNFO Shared Memory Footprint
// ***************************************************************************************************************
//
//         |-----------------------------|-----------------------------------|-----------------------------------|
// Content |  HWiNFO_SENSORS_SHARED_MEM2 |  HWiNFO_SENSORS_SENSOR_ELEMENT[]  | HWiNFO_SENSORS_READING_ELEMENT[]  |
//         |-----------------------------|-----------------------------------|-----------------------------------|
// Pointer |<--0                         |<--dwOffsetOfSensorSection         |<--dwOffsetOfReadingSection        |
//         |-----------------------------|-----------------------------------|-----------------------------------|
// Size    |  dwOffsetOfSensorSection    |   dwSizeOfSensorElement           |    dwSizeOfReadingElement         |
//         |                             |      * dwNumSensorElement         |       * dwNumReadingElement       |
//         |-----------------------------|-----------------------------------|-----------------------------------|
//
