namespace SimShift.Data.Common
{
    public interface IDataDefinition
    {
        float Time { get; }
        bool Paused { get; }
        int Gear { get; }
        int Gears { get; }
        float EngineRpm { get; }
        float Fuel { get; }
        float Throttle { get; }
        float Brake { get; }
        float Speed { get; }
    }
}