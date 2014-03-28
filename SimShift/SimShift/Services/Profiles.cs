using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SimShift.Utils;

namespace SimShift.Services
{
    public class Profiles : IConfigurable
    {
        public List<Profile> Loaded = new List<Profile>();
        public List<string> Unloaded = new List<string>();
        public string UniqueID { get; private set; }

        public string MasterFile { get; private set; }
        public string PatternFile { get; private set; }

        public Profiles(string app, string car)
        {
            UniqueID = string.Format("{0}.{1}", app, car);
            MasterFile = string.Format("Settings/Profiles/{0}.{1}.Master.ini", app, car);
            if (File.Exists(MasterFile) == false)
            {
                Debug.WriteLine("Cannot find " + MasterFile);
                return;
            }
            PatternFile = string.Format("Settings/Profiles/{0}.{1}.{2}.ini", app, car, "{0}");

            Main.Load(this, MasterFile);
        }

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs
        {
            get { return new string[] {"Profiles"}; }
        }

        public void ResetParameters()
        {
            Loaded.Clear();
            Unloaded.Clear();
        }

        public void Load(string profile)
        {
            if(Loaded.Any(x=>x.Name == profile))
            {
                Debug.WriteLine("Loading profile "+profile);
                Loaded.FirstOrDefault(x => x.Name == profile).Load();
            }
        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "Load":
                    var p = new Profile(this, obj.ReadAsString());
                    if(p.Loaded==false)
                    {
                        Unloaded.Add(obj.ReadAsString());
                    }
                    else
                    {
                        Loaded.Add(p);
                    }
                    break;

                case "Unload":
                    Unloaded.Add(obj.ReadAsString());
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            var obj = new List<IniValueObject>();

            foreach (var l in Loaded) obj.Add(new IniValueObject(AcceptsConfigs, "Load", l.Name));
            foreach (var l in Unloaded) obj.Add(new IniValueObject(AcceptsConfigs, "Load", l));

            return obj;
        }

        #endregion
    }
}