using System.Runtime.InteropServices;
using System.Text;
using SimShift.Data.Common;

namespace SimShift.Data
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Ets2DataDefinition
    {
        [FieldOffset(0)] public uint time;
        [FieldOffset(4)] public uint paused;

        [FieldOffset(8)] public uint ets2_telemetry_plugin_revision;
        [FieldOffset(12)] public uint ets2_version_major;
        [FieldOffset(16)] public uint ets2_version_minor;


        [FieldOffset(20)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] flags;

        // vehicle dynamics

        [FieldOffset(24)] public float speed;
        [FieldOffset(28)] public float accelerationX;
        [FieldOffset(32)] public float accelerationY;
        [FieldOffset(36)] public float accelerationZ;


        [FieldOffset(40)] public float coordinateX;
        [FieldOffset(44)] public float coordinateY;
        [FieldOffset(48)] public float coordinateZ;


        [FieldOffset(52)] public float rotationX;
        [FieldOffset(56)] public float rotationY;
        [FieldOffset(60)] public float rotationZ;

        // drivetrain essentials

        [FieldOffset(64)] public int gear;
        [FieldOffset(68)] public int gears;
        [FieldOffset(72)] public int gearRanges;
        [FieldOffset(76)] public int gearRangeActive;

        [FieldOffset(80)] public float engineRpm;
        [FieldOffset(84)] public float engineRpmMax;

        [FieldOffset(88)] public float fuel;
        [FieldOffset(92)] public float fuelCapacity;
        [FieldOffset(96)] public float fuelRate;
        [FieldOffset(100)] public float fuelAvgConsumption;

        // user input

        [FieldOffset(104)] public float userSteer;
        [FieldOffset(108)] public float userThrottle;
        [FieldOffset(112)] public float userBrake;
        [FieldOffset(116)] public float userClutch;


        [FieldOffset(120)] public float gameSteer;
        [FieldOffset(124)] public float gameThrottle;
        [FieldOffset(128)] public float gameBrake;
        [FieldOffset(132)] public float gameClutch;

        // truck & trailer

        [FieldOffset(136)] public float truckWeight;
        [FieldOffset(140)] public float trailerWeight;

        [FieldOffset(144)] public int modelOffset;
        [FieldOffset(148)] public int modelLength;

        [FieldOffset(152)] public int trailerOffset;
        [FieldOffset(156)] public int trailerLength;

        [FieldOffset(160)] public int timeAbsolute;
        [FieldOffset(164)] public int gearsReverse;

        [FieldOffset(168)] public float trailerMass;
        [FieldOffset(172)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] trailerId;
        [FieldOffset(236)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] trailerName;
        
        [FieldOffset(300)] public int jobIncome;
        [FieldOffset(304)] public int jobDeadline;
        
        [FieldOffset(308)] 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] jobCitySource;
        [FieldOffset(372)] 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] jobCityDestination;
        
        [FieldOffset(436)] 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] jobCompanySource;
        [FieldOffset(500)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] jobCompanyDestination;

        public IDataDefinition ToGeneric(string truck)
        {
            var generic = new GenericDataDefinition(truck, time/1000000.0f, (paused == 0 ? false : true), gear, gears,
                                                    engineRpm, fuel, gameThrottle,
                                                    gameBrake, speed);
            return generic;
        }
    }
}