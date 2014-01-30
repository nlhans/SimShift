using System.Collections.Generic;
using System.Linq;

namespace SimShift.Services
{
    public class ControlChain
    {
        private List<IControlChainObj> chain = new List<IControlChainObj>();

        private List<JoyControls> Axis = new List<JoyControls>();
        private List<JoyControls> Buttons = new List<JoyControls>();
 
        public ControlChain()
        {
            chain.Add(Main.Transmission);
            chain.Add(Main.Antistall);

            Axis.Add(JoyControls.Throttle);
            Axis.Add(JoyControls.Brake);
            Axis.Add(JoyControls.Clutch);

            Buttons.Add(JoyControls.Gear1);
            Buttons.Add(JoyControls.Gear2);
            Buttons.Add(JoyControls.Gear3);
            Buttons.Add(JoyControls.Gear4);
            Buttons.Add(JoyControls.Gear5);
            Buttons.Add(JoyControls.Gear6);
            Buttons.Add(JoyControls.GearR);
            Buttons.Add(JoyControls.GearRange1);
            Buttons.Add(JoyControls.GearRange2);
            Buttons.Add(JoyControls.GearUp);
            Buttons.Add(JoyControls.GearDown);
        }

        public void Tick()
        {
            // We take all controller input
            var buttonValues = Buttons.ToDictionary(c => c, Main.GetButtonIn);
            var axisValues = Axis.ToDictionary(c => c, Main.GetAxisIn);

            // Put it serially through each control block
            // Each time a block requires a control, it receives the current value of that control
            foreach(var obj in chain)
            {
                buttonValues = buttonValues.ToDictionary(c => c.Key, k => obj.Requires(k.Key) ? obj.GetButton(k.Key, k.Value) : k.Value);
                axisValues = axisValues.ToDictionary(c => c.Key, k => obj.Requires(k.Key) ? obj.GetAxis(k.Key, k.Value) : k.Value);
                obj.TickControls();
            }

            // And then put them onto our own controller.
            foreach (var b in buttonValues)
            {
                Main.SetButtonOut(b.Key, b.Value);
            }
            foreach (var b in axisValues)
            {
                Main.SetAxisOut(b.Key, b.Value);
            }
        }

    }

    public interface IControlChainObj
    {
        bool Requires(JoyControls c);
        double GetAxis(JoyControls c, double val);
        bool GetButton(JoyControls c, bool val);
        void TickControls();
    }
}