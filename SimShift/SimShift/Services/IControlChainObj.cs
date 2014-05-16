using SimShift.Data;
using SimShift.Data.Common;

namespace SimShift.Services
{
    public interface IControlChainObj
    {
        bool Requires(JoyControls c);
        double GetAxis(JoyControls c, double val);
        bool GetButton(JoyControls c, bool val);
        void TickControls();
        void TickTelemetry(IDataMiner data);
        bool Enabled { get; }
        bool Active { get; }
    }
}