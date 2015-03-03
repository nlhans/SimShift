using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using SimShift.Data;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public interface IControlChainObj
    {
        IEnumerable<string> SimulatorsOnly { get; }
        IEnumerable<string> SimulatorsBan { get; }

        bool Enabled { get; }
        bool Active { get; }

        bool Requires(JoyControls c);
        double GetAxis(JoyControls c, double val);
        bool GetButton(JoyControls c, bool val);
        void TickControls();
        void TickTelemetry(IDataMiner data);
    }
}