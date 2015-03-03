using System.Collections.Generic;
using SimShift.Data.Common;
using SimShift.Services;

namespace SimShift.Entities
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