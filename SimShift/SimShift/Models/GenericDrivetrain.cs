using System.Collections.Generic;
using SimShift.Utils;

namespace SimShift.Models
{
    public class GenericDrivetrain : IDrivetrain
    {
        internal struct GenericEngineData
        {
            public double N;
            public double P;

            public GenericEngineData(double n, double p)
            {
                N = n;
                P = p;
            }
        }

        private Dictionary<double, GenericEngineData> Engine = new Dictionary<double, GenericEngineData>();

        public GenericDrivetrain()
        {
            Calibrated = true;
        }

        public double StallRpm { get;  set; }
        public double MaximumRpm { get; set; }
        protected float GearReverse { get; set; }

        #region Implementation of IDrivetrain

        public virtual double CalculateTorqueN(double rpm)
        {
            var lastKey = 0.0;
            foreach (var r in Engine.Keys)
            {
                if (r > rpm && lastKey < rpm)
                {
                    var dutyCycle = (rpm - lastKey)/(r - lastKey);
                    return (Engine[r].N - Engine[lastKey].N)*dutyCycle + Engine[lastKey].N;
                }
                else
                {
                    lastKey = r;
                }
            }

            return 0;
        }

        public virtual double CalculateTorqueP(double rpm, double throttle)
        {
            var lastKey = 0.0;
            foreach (var r in Engine.Keys)
            {
                if (r > rpm && lastKey < rpm)
                {
                    var dutyCycle = (rpm - lastKey)/(r - lastKey);
                    return (Engine[r].P - Engine[lastKey].P)*dutyCycle + Engine[lastKey].P;
                }
                else
                {
                    lastKey = r;
                }
            }

            return 0;
        }

        public virtual  double CalculateThrottleByTorque(double rpm, double torque)
        {
            var torqueP = CalculateTorqueP(rpm, 1);
            if (torque > torqueP) return 1;
            var torqueN = CalculateTorqueN(rpm);
            if (torque < torqueN) return 0;

            var t = torque/(torqueP - torqueN);
            return t;

        }

        public virtual double CalculatePower(double rpm, double throttle)
        {
            return throttle*rpm;
        }

        public double CalculateMaxPower()
        {
            var pwr = 0.0;
            var pwrRpm = 0.0;
            for(var rpm = 0; rpm < MaximumRpm; rpm += 100)
            {
                var p = CalculatePower(rpm, 1);
                if(p>pwr)
                {
                    pwr = p;
                    pwrRpm = rpm;
                }
            }
            return pwr;
        }

        public virtual  double CalculateFuelConsumption(double rpm, double throttle)
        {
            var f = throttle*rpm/(MaximumRpm/2);
            if(rpm>MaximumRpm/2)
                return f*f*throttle;
            else
                return f*f*throttle;
        }


        public double CalculateThrottleByPower(double rpm, double powerRequired)
        {
            // 1 Nm @ 1000rpm = 0.1904hp
            // 1 Hp @ 1000rpm = 5.2521Nm
            if (rpm == 0) return 1;
            double torqueRequired = powerRequired/(rpm/1000)*(1/0.1904);
            return CalculateThrottleByTorque(rpm, torqueRequired);
        }

        public string File { get; set; }

        public double CalculateSpeedForRpm(int gear, float rpm)
        {
            if (GearRatios == null || gear < 0 || gear >= GearRatios.Length) return 0;
            return rpm / GearRatios[gear]/3.6;
        }

        public double CalculateRpmForSpeed(int gear, float speed)
        {
            if (GearRatios == null || gear < 0 || gear >= GearRatios.Length) return 0;
            return speed*3.6* (GearRatios[gear]);
        }

        public double[] GearRatios { get; set; }
        public int Gears { get; set; }
        public bool Calibrated { get; set; }

        #endregion

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs
        {
            get { return new[] {"Engine", "Gearbox"}; }
        }

        public virtual  void ResetParameters()
        {
            Engine = new Dictionary<double, GenericEngineData>();
            StallRpm = 900;
            MaximumRpm = 2500;
        }

        public virtual void ApplyParameter(IniValueObject obj)
        {
            if (obj.Group == "Engine")
            {
                switch (obj.Key)
                {
                    case "Idle":
                        StallRpm = obj.ReadAsFloat();
                        break;
                    case "Max":
                        MaximumRpm = obj.ReadAsFloat();
                        break;

                    case "Power":
                        Engine.Add(obj.ReadAsFloat(0), new GenericEngineData(obj.ReadAsFloat(1), obj.ReadAsFloat(2)));
                        break;
                }
            }
            else if (obj.Group == "Gearbox")
            {
                switch(obj.Key)
                {
                    case"Gears":
                        Gears = obj.ReadAsInteger();
                        GearRatios = new double[Gears];
                        break;

                    case "Gear":
                        GearRatios[obj.ReadAsInteger(0)] = obj.ReadAsFloat(1);
                        break;

                    case "GearR":
                        GearReverse = obj.ReadAsFloat();
                        break;

                }
            }
        
        }

        public virtual IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();

            obj.Add(new IniValueObject(new string[] {"Engine"}, "Idle", StallRpm.ToString()));
            obj.Add(new IniValueObject(new string[] {"Engine"}, "Max", MaximumRpm.ToString()));
            foreach (var frame in Engine)
                obj.Add(new IniValueObject(new string[] {"Engine"}, "Power",
                                           string.Format("({0},{1},{2})", frame.Key, frame.Value.N, frame.Value.P)));

            obj.Add(new IniValueObject(new string[] {"Gearbox"}, "Gears", Gears.ToString()));
            obj.Add(new IniValueObject(new string[] {"Gearbox"}, "GearR", GearReverse.ToString()));
            for (int g = 0; g < Gears; g++)
                obj.Add(new IniValueObject(new string[] {"Gearbox"}, "Gear",
                                           string.Format("({0},{1})", g, GearRatios[g])));

            return obj;
        }

        #endregion
    }
}