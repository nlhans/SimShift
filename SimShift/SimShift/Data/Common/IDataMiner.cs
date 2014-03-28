using System;
using System.Diagnostics;

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

        void EvtStart();
        void EvtStop();
    }
}