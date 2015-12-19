using System.Collections.Generic;
using System.Linq;

namespace SimShift.MapTool
{
    public class Ets2PrefabRoute
    {
        public IndicatorSignal Indicator;

        public List<Ets2PrefabCurve> Route { get; private set; }

        public int Start { get { return Route.FirstOrDefault().Index; } }
        public int End { get { return Route.LastOrDefault().Index; } }

        public Ets2PrefabNode Entry { get; private set; }
        public Ets2PrefabNode Exit { get; private set; }

        public Ets2PrefabRoute(List<Ets2PrefabCurve> route, Ets2PrefabNode entry, Ets2PrefabNode exit)
        {
            Route = route;

            Entry = entry;
            Exit = exit;

            // TODO: Interpret indicator signal

        }

        public override string ToString()
        {
            return "Prefab route " + Start + " to  " + End;
        }
    }
}