namespace SimShift.Entities
{
    public class ShiftPatternFrame
    {
        public double Clutch { get; private set; }
        public double Throttle { get; private set; }
        public bool AbsoluteThrottle { get; private set; }
        public bool UseOldGear { get; private set; }
        public bool UseNewGear { get; private set; }

        public ShiftPatternFrame(double clutch, double throttle, bool useOldGear, bool useNewGear)
        {
            Clutch = clutch;
            Throttle = throttle;
            AbsoluteThrottle = false;
            UseOldGear = useOldGear;
            UseNewGear = useNewGear;
        }
        public ShiftPatternFrame(double clutch, double throttle, bool absThr, bool useOldGear, bool useNewGear)
        {
            Clutch = clutch;
            Throttle = throttle;
            AbsoluteThrottle = absThr;
            UseOldGear = useOldGear;
            UseNewGear = useNewGear;
        }
    }
}