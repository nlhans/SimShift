namespace SimShift.Dialogs
{
    public class ShifterTableLookupResult
    {
        public int Gear { get; private set; }

        public double UsedSpeed { get; private set; }
        public double UsedLoad { get; private set; }

        public ShifterTableLookupResult(int gear, double usedSpeed, double usedLoad)
        {
            Gear = gear;
            UsedSpeed = usedSpeed;
            UsedLoad = usedLoad;
        }
    }
}