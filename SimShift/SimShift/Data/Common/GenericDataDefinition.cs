namespace SimShift.Data.Common
{
    public class GenericDataDefinition : IDataDefinition
    {
        public GenericDataDefinition(float time, bool paused, int gear, int gears, float engineRpm, float fuel, float throttle, float brake, float speed)
        {
            Time = time;
            Paused = paused;
            Gear = gear;
            Gears = gears;
            EngineRpm = engineRpm;
            Fuel = fuel;
            Throttle = throttle;
            Brake = brake;
            Speed = speed;
        }

        public float Time { get; private set; }
        public bool Paused { get; private set; }

        public int Gear { get; private set; }
        public int Gears { get; private set; }

        public float EngineRpm { get; private set; }
        public float Fuel { get; private set; }

        public float Throttle { get; private set; }
        public float Brake { get; private set; }

        public float Speed { get; private set; }
    }
}