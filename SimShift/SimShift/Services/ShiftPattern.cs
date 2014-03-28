using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimShift.Utils;

namespace SimShift.Services
{
    public class ShiftPattern : IConfigurable
    {
        public List<ShiftPatternFrame> Frames = new List<ShiftPatternFrame>();

        public int Count
        {
            get { return Frames.Count; }
        }

        #region Implementation of IConfigurable

        public IEnumerable<string> AcceptsConfigs
        {
            get { return new[] {"Pattern"}; }
        }

        public void ResetParameters()
        {
            Frames = new List<ShiftPatternFrame>();

        }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "Frame":
                    var thr = obj.ReadAsFloat(0);
                    var clt = obj.ReadAsFloat(1);
                    var thrAbs = obj.ReadAsString(2);
                    var gear = obj.ReadAsString(3);

                    var useNewGear = gear == "new";
                    var useOldGear = gear == "old";
                    var absoluteThrottle = thrAbs == "abs";

                    var frame = new ShiftPatternFrame(clt, thr, absoluteThrottle, useOldGear, useNewGear);

                    Frames.Add(frame);
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            var frames = new List<IniValueObject>();

            foreach(var fr in Frames)
            {
                string absThrStr = fr.AbsoluteThrottle ? "abs" : "rel";
                string gearStr = fr.UseNewGear ? "new" : fr.UseOldGear ? "old" : "neu";

                frames.Add(new IniValueObject(AcceptsConfigs, "Frame", string.Format("Frame=({0:0.00},{1:0.00},{2},{3})", fr.Throttle, fr.Clutch, absThrStr, gearStr)));
            }

            return frames;
        }

        #endregion
    }
}
