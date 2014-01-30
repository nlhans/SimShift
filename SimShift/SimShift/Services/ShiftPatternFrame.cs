namespace SimShift.Services
{
    public class ShiftPatternFrame
    {
        public double Clutch { get; private set; }
        public double Throttle { get; private set; }

        public bool UseOldGear { get; private set; }
        public bool UseNewGear { get; private set; }

        public ShiftPatternFrame(double clutch, double throttle, bool useOldGear, bool useNewGear)
        {
            Clutch = clutch;
            Throttle = throttle;
            UseOldGear = useOldGear;
            UseNewGear = useNewGear;
        }
    }
}