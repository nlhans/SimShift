using System;
using System.Diagnostics;
using SimShift.Services;

namespace SimShift.Data.Common
{
    public interface IDataMiner
    {
        string Application { get; }
        string Name { get; }

        EventHandler DataReceived { get; set; }
        IDataDefinition Telemetry { get; }
        bool Running { get; set; }
        bool IsActive { get; set; }
        bool SelectManually { get; }
        bool RunEvent { get; set; }
        Process ActiveProcess { get; set; }

        bool SupportsCar { get; }
        bool TransmissionSupportsRanges { get; }
        bool EnableWeirdAntistall { get; }

        void EvtStart();
        void EvtStop();
        void Write<T>(TelemetryChannel cameraHorizon, T i);
    }
}