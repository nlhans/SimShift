using System;
using System.IO;

namespace SimShift.MapTool
{
    public class Ets2RoadLook
    {
        public bool IsHighway { get; private set; }
        public bool IsLocal { get; private set; }
        public bool IsExpress { get; private set; }
        public string LookID { get; private set; }
        private Ets2Mapper Mapper;

        public float Offset;
        public float SizeLeft;
        public float SizeRight;
        public float ShoulderLeft;
        public float ShoulderRight;

        public int LanesLeft;
        public int LanesRight;

        public Ets2RoadLook(string look, Ets2Mapper mapper)
        {
            LookID = look;
            Mapper = mapper;

            var roadLookData = mapper.LUTFolder + "-roadlook.sii";
            var fileData = File.ReadAllLines(roadLookData);

            var found = false;
            foreach (var k in fileData)
            {
                if (!found)
                {
                    if (k.StartsWith("road_look") && k.Contains(LookID))
                    {
                        found = true;
                    }
                }
                else
                {
                    //value:
                    if (k.Contains(":"))
                    {
                        var key = k;
                        var data = key.Substring(key.IndexOf(":")+1).Trim();
                        key = key.Substring(0, key.IndexOf(":")).Trim();

                        switch (key)
                        {
                            case "road_size_left":
                                float.TryParse(data, out SizeLeft);
                                break;

                            case "road_size_right":
                                float.TryParse(data, out SizeRight);
                                break;

                            case "shoulder_size_right":
                                float.TryParse(data, out ShoulderLeft);
                                break;

                            case "shoulder_size_left":
                                float.TryParse(data, out ShoulderRight);
                                break;

                            case "road_offset":
                                float.TryParse(data, out Offset);
                                break;
                            case "lanes_left[]":
                                LanesLeft++;
                                IsLocal = (data == "traffic_lane.road.local");
                                IsExpress = (data == "traffic_lane.road.expressway");
                                IsHighway = (data == "traffic_lane.road.motorway");

                                break;

                            case "lanes_right[]":
                                LanesRight++;
                                break;
                        }
                    }
                    if (k.Trim() == "}")
                        break;
                }
            }
        }
    }
}