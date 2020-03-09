using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace SensorMonHTTP 
{
    public class HWiNFOWrapper
    {
        public const string HWiNFO_SENSORS_MAP_FILE_NAME2 = "Global\\HWiNFO_SENS_SM2";
        public const string HWiNFO_SENSORS_SM2_MUTEX = "Global\\HWiNFO_SM2_MUTEX";
        public const int HWiNFO_SENSORS_STRING_LEN2 = 128;
        public const int HWiNFO_UNIT_STRING_LEN = 16;

        public enum SENSOR_READING_TYPE
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

  
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct _HWiNFO_SENSORS_READING_ELEMENT
        {
            public SENSOR_READING_TYPE tReading;
            public UInt32 dwSensorIndex;
            public UInt32 dwReadingID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            public string szLabelOrig;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            public string szLabelUser;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_UNIT_STRING_LEN)]
            public string szUnit;
            public double Value;
            public double ValueMin;
            public double ValueMax;
            public double ValueAvg;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct _HWiNFO_SENSORS_SENSOR_ELEMENT
        {
            public UInt32 dwSensorID;
            public UInt32 dwSensorInst;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            public string szSensorNameOrig;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWiNFO_SENSORS_STRING_LEN2)]
            public string szSensorNameUser;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct _HWiNFO_SENSORS_SHARED_MEM2
        {
            public UInt32 dwSignature;
            public UInt32 dwVersion;
            public UInt32 dwRevision;
            public long poll_time;
            public UInt32 dwOffsetOfSensorSection;
            public UInt32 dwSizeOfSensorElement;
            public UInt32 dwNumSensorElements;
            // descriptors for the Readings section
            public UInt32 dwOffsetOfReadingSection; // Offset of the Reading section from beginning of HWiNFO_SENSORS_SHARED_MEM2
            public UInt32 dwSizeOfReadingElement;   // Size of each Reading element = sizeof( HWiNFO_SENSORS_READING_ELEMENT )
            public UInt32 dwNumReadingElements;     // Number of Reading elements

            public bool Equals(_HWiNFO_SENSORS_SHARED_MEM2 obj)
            {
                bool areEqual =
                    dwSignature == obj.dwSignature &&
                    dwVersion == obj.dwVersion &&
                    dwRevision == obj.dwRevision &&
                    poll_time == obj.poll_time &&
                    dwOffsetOfSensorSection == obj.dwOffsetOfSensorSection &&
                    dwSizeOfSensorElement == obj.dwSizeOfSensorElement &&
                    dwNumSensorElements == obj.dwNumSensorElements &&
                    dwOffsetOfReadingSection == obj.dwOffsetOfReadingSection &&
                    dwSizeOfReadingElement == obj.dwSizeOfReadingElement &&
                    dwNumReadingElements == obj.dwNumReadingElements;
                return areEqual;
            }
        };
    

	    MemoryMappedFile mmf;
        //private uint numSensors;
        //private uint numReadingElements;
        private List<string> masterSensorNames;

        //private uint offsetSensorSection;
        //private uint sizeSensorElement;
        //private uint offsetReadingSection;
        //private uint sizeReadingSection;

        // ***************************************************************************************************************
        //                                                  Code Example
        // ***************************************************************************************************************

        public class Console
        {
            public static Action<string> WriteLine = null;

            public static Action ReadLine = null;
        }


        public class AccessorInfo
        {
            public MemoryMappedViewAccessor Accessor;
            public _HWiNFO_SENSORS_SHARED_MEM2 MemInfo = new _HWiNFO_SENSORS_SHARED_MEM2();
            public byte[] SensorData = null;
            public GCHandle SensorHandle;
            public byte[] ReadingData = null;
            public GCHandle ReadingHandle;

            public void FreeBuffers()
            {
                if (SensorData != null)
                {
                    SensorHandle.Free();
                    SensorData = null;
                }

                if (ReadingData != null)
                {
                    ReadingHandle.Free();
                    ReadingData = null;
                }

            }
        }

        public AccessorInfo CurrentAccessorInfo = new AccessorInfo();

        public AccessorInfo GetValidAccessorInfo()
        {
            var accessorInfo = CurrentAccessorInfo;
            var accessor = accessorInfo.Accessor;
            if (accessor == null)
            {
                accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf(typeof(_HWiNFO_SENSORS_SHARED_MEM2)),
                    MemoryMappedFileAccess.Read);
                CurrentAccessorInfo.Accessor = accessor;
            }

            _HWiNFO_SENSORS_SHARED_MEM2 memInfo;
            accessor.Read(0, out memInfo);


            accessorInfo.MemInfo = memInfo;
            var sizeSensorElement = memInfo.dwSizeOfSensorElement;
            var sizeOfReadingElement = memInfo.dwSizeOfReadingElement;

            if (accessorInfo.SensorData == null || accessorInfo.SensorData.Length < sizeSensorElement)
            {
                if (accessorInfo.SensorData != null)
                {
                    accessorInfo.SensorHandle.Free();
                }

                var sensorData = new byte[sizeSensorElement];
                var sensorHandle = GCHandle.Alloc(sensorData, GCHandleType.Pinned);
                    
                accessorInfo.SensorData = sensorData;
                accessorInfo.SensorHandle = sensorHandle;
            }

            if (accessorInfo.ReadingData == null || accessorInfo.ReadingData.Length < sizeOfReadingElement)
            {
                if (accessorInfo.ReadingData != null)
                {
                    accessorInfo.ReadingHandle.Free();
                }

                var readingData = new byte[sizeOfReadingElement];
                var readingHandle = GCHandle.Alloc(readingData, GCHandleType.Pinned);

                accessorInfo.ReadingData = readingData;
                accessorInfo.ReadingHandle = readingHandle;
            }

            return accessorInfo;
        }

        public void Open(bool writeConsole = true)
        {
            masterSensorNames = new List<string>();
            try
            {
                mmf = MemoryMappedFile.OpenExisting(HWiNFO_SENSORS_MAP_FILE_NAME2, MemoryMappedFileRights.Read);
            }
		    catch (Exception Ex)
            {
                Console.WriteLine("An error occured while opening the HWiNFO shared memory! - " + Ex.Message);
                Console.WriteLine("Press ENTER to exit program...");
                Console.ReadLine();
                Environment.Exit(1);
            }
            //using (var accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf(typeof(_HWiNFO_SENSORS_SHARED_MEM2)), MemoryMappedFileAccess.Read))
            var accessorInfo = GetValidAccessorInfo();
            var accessor = accessorInfo.Accessor;
            {
                /*
			    _HWiNFO_SENSORS_SHARED_MEM2 HWiNFOMemory ;
                accessor.Read(0, out HWiNFOMemory);
			    numSensors = HWiNFOMemory.dwNumSensorElements ;
                numReadingElements = HWiNFOMemory.dwNumReadingElements;
			    offsetSensorSection = HWiNFOMemory.dwOffsetOfSensorSection ;
                sizeSensorElement = HWiNFOMemory.dwSizeOfSensorElement;
                offsetReadingSection = HWiNFOMemory.dwOffsetOfReadingSection;
                sizeReadingSection = HWiNFOMemory.dwSizeOfReadingElement;
                */
                var memInfo = accessorInfo.MemInfo;
                var numSensors = memInfo.dwNumSensorElements;
                var numReadingElements = memInfo.dwNumReadingElements;
                var offsetSensorSection = memInfo.dwOffsetOfSensorSection;
                var sizeSensorElement = memInfo.dwSizeOfSensorElement;
                var offsetReadingSection = memInfo.dwOffsetOfReadingSection;
                var sizeReadingSection = memInfo.dwSizeOfReadingElement;

                using (var sensor_element_accessor = mmf.CreateViewStream(
                    // offsetSensorSection + (dwSensor * sizeSensorElement), sizeSensorElement,
                    offsetSensorSection + 0, numSensors * sizeSensorElement,
                    MemoryMappedFileAccess.Read))
                {
                    sensor_element_accessor.Seek(0, SeekOrigin.Begin);
                    for (UInt32 dwSensor = 0; dwSensor < numSensors; dwSensor++)
                    {

                        //byte[] byteBuffer = new byte[sizeSensorElement];
                        var byteBuffer = accessorInfo.SensorData;

                        // GCHandle handle = GCHandle.Alloc(byteBuffer, GCHandleType.Pinned);
                        sensor_element_accessor.Read(byteBuffer, 0, (int) sizeSensorElement);
                        //GCHandle handle = GCHandle.Alloc(byteBuffer, GCHandleType.Pinned);
                        var handle = accessorInfo.SensorHandle;
                        _HWiNFO_SENSORS_SENSOR_ELEMENT SensorElement =
                            (_HWiNFO_SENSORS_SENSOR_ELEMENT) Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
                                typeof(_HWiNFO_SENSORS_SENSOR_ELEMENT));

                        //handle.Free();

                        masterSensorNames.Add(SensorElement.szSensorNameUser);

                        if (writeConsole)
                        {
                            Console.WriteLine(String.Format("dwSensorID : {0}", SensorElement.dwSensorID));
                            Console.WriteLine(String.Format("dwSensorInst : {0}", SensorElement.dwSensorInst));
                            Console.WriteLine(String.Format("szSensorNameOrig : {0}", SensorElement.szSensorNameOrig));
                            Console.WriteLine(String.Format("szSensorNameUser : {0}", SensorElement.szSensorNameUser));
                        }
                    }
                }

                //                using (var sensor_element_accessor = mmf.CreateViewStream(
                //                    offsetReadingSection + (dwReading * sizeReadingSection), sizeReadingSection,
                //                    MemoryMappedFileAccess.Read))
                using (var sensor_element_accessor = mmf.CreateViewStream(
                    offsetReadingSection, sizeReadingSection * numReadingElements, MemoryMappedFileAccess.Read))
                {
                    sensor_element_accessor.Seek(0, SeekOrigin.Begin);
                    for (UInt32 dwReading = 0; dwReading < numReadingElements; dwReading++)
                    {
                        //byte[] byteBuffer = new byte[sizeReadingSection];
                        //GCHandle handle = GCHandle.Alloc(byteBuffer, GCHandleType.Pinned);

                        var byteBuffer = accessorInfo.ReadingData;

                        sensor_element_accessor.Read(byteBuffer, 0, (int) sizeReadingSection);
                        //sensor_element_accessor.Read(byteBuffer, 0, (int) sizeReadingSection);
                        //GCHandle handle = GCHandle.Alloc(byteBuffer, GCHandleType.Pinned);
                        var handle = accessorInfo.ReadingHandle;
                        _HWiNFO_SENSORS_READING_ELEMENT ReadingElement =
                            (_HWiNFO_SENSORS_READING_ELEMENT) Marshal.PtrToStructure(handle.AddrOfPinnedObject(),
                                typeof(_HWiNFO_SENSORS_READING_ELEMENT));
                        //handle.Free();

                        if (writeConsole)
                        {
                            Console.WriteLine(String.Format("tReading : {0}", ReadingElement.tReading));
                            Console.WriteLine(String.Format("dwSensorIndex : {0} ; Sensor Name: {1}",
                                ReadingElement.dwSensorIndex, masterSensorNames[(int) ReadingElement.dwSensorIndex]));
                            Console.WriteLine(String.Format("dwReadingID : {0}", ReadingElement.dwSensorIndex));
                            Console.WriteLine(String.Format("szLabelUser : {0}", ReadingElement.szLabelUser));
                            Console.WriteLine(String.Format("szUnit : {0}", ReadingElement.szUnit));
                            Console.WriteLine(String.Format("Value : {0}", ReadingElement.Value));
                        }
                    }
                }
            }
 	    }
	    public void Close(bool closeBuffers = false)
        {
            if (mmf != null)
            {
                mmf.Dispose();
            }

            if(closeBuffers)
                CurrentAccessorInfo?.FreeBuffers();
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
