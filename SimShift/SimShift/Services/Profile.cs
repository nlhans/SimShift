using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SimShift.Utils;

namespace SimShift.Services
{
    public class Profile : IConfigurable
    {
        public bool Loaded = false;
        public string Name { get; private set; }

        public string Antistall { get; private set; }
        public string CruiseControl { get; private set; }
        public string ShiftCurve { get; private set; }
        public List<ConfigurableShiftPattern> ShiftPattern { get; private set; }
        public string SpeedLimiter { get; private set; }

        private Profiles Car { get; set; }

        public Profile(Profiles car, string file)
        {
            Name = file;
            Car = car;

            var phFile = string.Format(car.PatternFile, file);

            if (!File.Exists(phFile))
                Debug.WriteLine("Cannot find file " + phFile);
            else
            {
                Loaded = true;
            }

            Main.Load(this, phFile);
        }

        public void Load()
        {
            //
            Main.Load(Main.Antistall, "Settings/Antistall/" + Antistall + ".ini");
            Main.Load(Main.CruiseControl, "Settings/CruiseControl/" + CruiseControl + ".ini");
            Main.Load(Main.Drivetrain, "Settings/Drivetrain/" + Car.UniqueID + ".ini");
            Main.Load(Main.Transmission, "Settings/ShiftCurve/" + ShiftCurve + ".ini");
            Main.Load(Main.Speedlimiter, "Settings/SpeedLimiter/" + SpeedLimiter + ".ini");
        }

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs { get { return new string[] { "Profiles"};}}
        public void ResetParameters()
        {
            ShiftPattern= new List<ConfigurableShiftPattern>();
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch(obj.Key)
            {
                case "Antistall":
                    Antistall = obj.ReadAsString();
                    break;
                case "CruiseControl":
                    CruiseControl= obj.ReadAsString();
                    break;
                case "ShiftCurve":
                    ShiftCurve = obj.ReadAsString();
                    break;
                case "SpeedLimiter":
                    SpeedLimiter= obj.ReadAsString();
                    break;
                case "ShiftPattern":
                    var file = obj.ReadAsString(2);
                    var region = obj.ReadAsString(0).ToLower() + "_" + obj.ReadAsString(1) + "thr";
                    ShiftPattern.Add(new ConfigurableShiftPattern(region, file));
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();
            obj.Add(new IniValueObject(AcceptsConfigs, "Antistall", Antistall));
            obj.Add(new IniValueObject(AcceptsConfigs, "CruiseControl", CruiseControl));
            obj.Add(new IniValueObject(AcceptsConfigs, "ShiftCurve", ShiftCurve));
            obj.Add(new IniValueObject(AcceptsConfigs, "SpeedLimiter", SpeedLimiter));
            foreach (var s in ShiftPattern)
            {
                if(s.Region.IndexOf("_")<0) continue;
                var part = s.Region.Substring(0, s.Region.IndexOf("_"));
                var thr = s.Region.Substring(s.Region.IndexOf("_") + 1);
                obj.Add(new IniValueObject(AcceptsConfigs, "ShiftPattern", string.Format("({0},{1},{2})", part, thr, s.File)));
            }

            return obj;
        }

        #endregion
    }
}