using System;
using System.Diagnostics;
using SimShift.Services;

namespace SimShift.Data.Common
{
    public interface IDataMiner
    {
        string Application { get; }

        EventHandler DataReceived { get; set; }
        IDataDefinition Telemetry { get; }
        bool Running { get; set; }
        bool IsActive { get; set; }
        bool RunEvent { get; set; }
        Process ActiveProcess { get; set; }

        bool TransmissionSupportsRanges { get; }
        bool EnableWeirdAntistall { get; }
        double Weight { get;  }

        void EvtStart();
        void EvtStop();
        void Write<T>(TelemetryChannel cameraHorizon, T i);
    }
}