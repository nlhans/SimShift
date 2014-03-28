using System.Linq;
using SimShift.Services;

namespace SimShift.Models
{
    public interface IDrivetrain : IConfigurable
    {
        double StallRpm { get; }
        double MaximumRpm { get; }
        double CalculateTorqueN(double rpm);
        double CalculateTorqueP(double rpm, double throttle);
        double CalculateThrottleByTorque(double rpm, double torque);
        double CalculatePower(double rpm, double throttle);
        double CalculateFuelConsumption(double rpm, double throttle);
        double CalculateThrottleByPower(double rpm, double powerRequired);

        double[] GearRatios { get; }
        int Gears { get; }
    }
}