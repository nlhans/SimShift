using SimShift.Data;

namespace SimShift.Services
{
    public interface IControlChainObj
    {
        bool Requires(JoyControls c);
        double GetAxis(JoyControls c, double val);
        bool GetButton(JoyControls c, bool val);
        void TickControls();
        void TickTelemetry(Ets2DataMiner data);
    }
}