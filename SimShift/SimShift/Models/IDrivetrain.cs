using System.Linq;
using SimShift.Services;

namespace SimShift.Models
{
    public interface IDrivetrain : IConfigurable
    {
        double StallRpm { get; set; }
        double MaximumRpm { get; set; }
        double CalculateTorqueN(double rpm);
        double CalculateTorqueP(double rpm, double throttle);
        double CalculateThrottleByTorque(double rpm, double torque);
        double CalculatePower(double rpm, double throttle);
        double CalculateMaxPower();
        double CalculateFuelConsumption(double rpm, double throttle);
        double CalculateThrottleByPower(double rpm, double powerRequired);

        double[] GearRatios { get; set; }
        int Gears { get; set; }
        bool Calibrated { get; set; }
        string File { get; set; }
        double CalculateSpeedForRpm(int gear, float rpm);
    }
}