using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;
using SimShift.Map.Splines;
using CubicSpline = MathNet.Numerics.Interpolation.CubicSpline;

namespace SimShift.MapTool
{
    public class Ets2Mapper
    {
        public bool Loading { get; private set; }

        public string SectorFolder { get; private set; }
        public string[] SectorFiles { get; private set; }

        public string PrefabFolder { get; private set; }
        public string[] PrefabFiles { get; private set; }

        public string LUTFolder { get; private set; }

        public List<Ets2Sector> Sectors { get; private set; }

        public ConcurrentDictionary<ulong, Ets2Node> Nodes = new ConcurrentDictionary<ulong, Ets2Node>();
        public ConcurrentDictionary<ulong, Ets2Item> Items = new ConcurrentDictionary<ulong, Ets2Item>();

        public Dictionary<string, Ets2Item> Cities = new Dictionary<string, Ets2Item>();
        public Dictionary<Tuple<string, string>, Ets2Item> Companies = new Dictionary<Tuple<string, string>, Ets2Item>();

        /***  SOME ITEMS CROSS MULTIPLE SECTORS; PENDING SEARCH REQUESTS ***/
        private List<Ets2ItemSearchRequest> ItemSearchRequests { get; set; }

        /*** VARIOUS LOOK UP TABLES (LUTs) TO FIND CERTAIN GAME ITEMS ***/ 
        internal List<Ets2Prefab> PrefabsLookup = new List<Ets2Prefab>();
        private List<Ets2Company> CompaniesLookup = new List<Ets2Company>();
        private Dictionary<int, Ets2Prefab> PrefabLookup = new Dictionary<int, Ets2Prefab>();
        private Dictionary<ulong, string> CitiesLookup = new Dictionary<ulong, string>();
        private Dictionary<uint, Ets2RoadLook> RoadLookup = new Dictionary<uint, Ets2RoadLook>(); 

        public Ets2Mapper(string sectorFolder, string prefabFolder, string lut)
        {
            SectorFolder = sectorFolder;
            PrefabFolder = prefabFolder;

            SectorFiles = Directory.GetFiles(sectorFolder, "*.base");
            PrefabFiles = Directory.GetFiles(prefabFolder, "*.ppd", SearchOption.AllDirectories);

            LUTFolder = lut;
        }

        public  Ets2Item FindClosestRoadPrefab(PointF location)
        {
            // Find road or prefab closest by
            var closestPrefab =
                Items.Values.Where(x => x.HideUI==false && x.Type == Ets2ItemType.Prefab && x.Prefab != null && x.Prefab.Curves.Any())
                    .OrderBy(x => Math.Sqrt(Math.Pow(location.X - x.PrefabNode.X, 2) + Math.Pow(location.Y - x.PrefabNode.Z, 2)))
                    .FirstOrDefault();
            return closestPrefab;
        }
        
        /// <summary>
        /// Navigate from X/Y to X/Y coordinates
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public Ets2NavigationRoute NavigateTo(PointF from, PointF to)
        {
            var start = FindClosestRoadPrefab(from);
            var end = FindClosestRoadPrefab(to);

            Console.WriteLine("Navigating from " + start.ItemUID.ToString("X16") + " to " + end.ItemUID.ToString("X16"));
            // Look up pre-fab closest by these 2 points
            return new Ets2NavigationRoute(start,end, new Ets2Point(from), new Ets2Point(to), this);
        }

        /// <summary>
        /// Navigate to city from X/Y point
        /// </summary>
        /// <param name="from"></param>
        /// <param name="city"></param>
        public Ets2NavigationRoute NavigateTo(PointF from, string city)
        {
            if (Cities.ContainsKey(city) == false)
                return null;
            
            var cityPoint = new PointF(Cities[city].StartNode.X, Cities[city].StartNode.Z);

            var start = FindClosestRoadPrefab(from);
            var end = FindClosestRoadPrefab(cityPoint);

            return new Ets2NavigationRoute(start,end, new Ets2Point(from), null, this);
        }
        /// <summary>
        /// Navigate to city company from X/Y point
        /// </summary>
        /// <param name="from"></param>
        /// <param name="city"></param>
        /// <param name="company"></param>
        public Ets2NavigationRoute NavigateTo(PointF from, string city, string company)
        {
            throw new NotImplementedException();
        }

        private void LoadLUT()
        {
            // PREFABS
            var sii = LUTFolder + "-prefab.sii";
            var csv = LUTFolder + "-prefab.csv";

            Dictionary<string, string> prefab2file = new Dictionary<string, string>();
            Dictionary<int, string> idx2prefab = new Dictionary<int, string>();
            var csvLines = File.ReadAllLines(csv);
            foreach (var k in csvLines)
            {
                var d = k.Split(",".ToCharArray());
                int idx;

                if (int.TryParse(d[2], NumberStyles.HexNumber, null, out idx))
                {
                    if (idx2prefab.ContainsKey(idx) == false)
                    idx2prefab.Add(idx, d[1]);
                }
            }

            var siiLines = File.ReadAllLines(sii);

            var prefab = string.Empty;
            var file = string.Empty;
            foreach (var k in siiLines)
            {
                if (k.Trim() == "}")
                {
                    if (prefab != string.Empty && file != string.Empty)
                    {
                        prefab2file.Add(prefab, file);
                    }

                    prefab = string.Empty;
                    file = string.Empty;
                }

                if (k.StartsWith("prefab_model"))
                {
                    var d = k.Split(":".ToCharArray()).Select(x => x.Trim()).ToArray();
                    if (d[1].Length > 3)
                    {
                        prefab = d[1].Substring(0, d[1].Length - 1).Trim();
                    }
                }

                if (k.Contains("prefab_desc"))
                {
                    var d = k.Split("\"".ToCharArray());
                    file = d[1].Trim();
                }

            }

            // Link all prefabs
            foreach (var id2fab in idx2prefab)
            {
                if (prefab2file.ContainsKey(id2fab.Value))
                {
                    var f = prefab2file[id2fab.Value];
                    var obj = PrefabsLookup.FirstOrDefault(x => x.IsFile(f));

                    if (obj != null)
                    {
                        obj.IDX = id2fab.Key;
                        obj.IDSII = id2fab.Value;

                        PrefabLookup.Add(id2fab.Key, obj);
                    }
                }
            }

            // COMPANIES
            CompaniesLookup = File.ReadAllLines(LUTFolder + "-companies.csv").Select(x => new Ets2Company(x, this)).ToList();

            // CITIES
            CitiesLookup = File.ReadAllLines(LUTFolder + "-cities.csv").Select(x =>
            {
                var d = x.Split(",".ToCharArray());
                var id = ulong.Parse(d[0], NumberStyles.HexNumber);
                var city = d[1];
                return new KeyValuePair<ulong, string>(id, city);
            }).ToDictionary(x => x.Key, x => x.Value);

            // ROAD LOOKS
            RoadLookup = File.ReadAllLines(LUTFolder + "-roads.csv").Select(x =>
            {
                var d = x.Split(",".ToCharArray());
                var id = uint.Parse(d[0], NumberStyles.HexNumber);
                var look = d[1];
                var lookObj = new Ets2RoadLook(look,this);
                return new KeyValuePair<uint, Ets2RoadLook>(id, lookObj);
            }).ToDictionary(x => x.Key, x => x.Value);
        }

        public void Parse()
        {
            ThreadPool.SetMaxThreads(2, 2);
            Loading = true;

            // First load prefabs
            PrefabsLookup = PrefabFiles.Select(x => new Ets2Prefab(this, x)).ToList();

            // Load all LUTs
            LoadLUT();

            ItemSearchRequests = new List<Ets2ItemSearchRequest>();
            Sectors = SectorFiles.Select(x => new Ets2Sector(this, x)).ToList();

            // 2-stage process so we can validate node UID's at item stage
            ThreadPool.SetMaxThreads(1, 1);
            Parallel.ForEach(Sectors, (sec) => sec.ParseNodes());
            Parallel.ForEach(Sectors, (sec) => sec.ParseItems());

            Loading = false;

            // Now find all that were not ofund
            ItemSearchRequests.Clear();
            Console.WriteLine(ItemSearchRequests.Count +
                              " were not found; attempting to search them through all sectors");
            foreach (var req in ItemSearchRequests)
            {
                Ets2Item item = Sectors.Select(sec => sec.FindItem(req.ItemUID)).FirstOrDefault(tmp => tmp != null);

                if (item == null)
                {
                    Console.WriteLine("Still couldn't find node " + req.ItemUID.ToString("X16"));
                }
                else
                {
                    if (req.IsBackward)
                    {
                        item.Apply(req.Node);
                        req.Node.BackwardItem = item;
                    }
                    if (req.IsForward)
                    {
                        item.Apply(req.Node);
                        req.Node.ForwardItem = item;
                    }

                    if (item.StartNode == null && item.StartNodeUID != null)
                    {
                        Ets2Node startNode;
                        if (Nodes.TryGetValue(item.StartNodeUID, out startNode))
                            item.Apply(startNode);
                    }
                    if (item.EndNode == null && item.EndNodeUID != null)
                    {
                        Ets2Node endNode;
                        if (Nodes.TryGetValue(item.EndNodeUID, out endNode))
                            item.Apply(endNode);
                    }

                    Console.Write(".");
                }
            }

            // Navigation cache
            BuildNavigationCache();

            // Lookup all cities
            Cities = Items.Values.Where(x => x.Type == Ets2ItemType.City).GroupBy(x=>x.City).Select(x=>x.FirstOrDefault()).ToDictionary(x => x.City, x => x);

            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Building) + " buildings were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Road) + " roads were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Prefab) + " prefabs were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Prefab && x.Prefab != null && x.Prefab.Curves.Any()) + " road prefabs were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Service) + " service points were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.Company) + " companies were found");
            Console.WriteLine(Items.Values.Count(x => x.Type == Ets2ItemType.City) + " cities were found");
        }

        private void BuildNavigationCache()
        {
            // The idea of navigation cache is that we calculate distances between nodes
            // The nodes we identify as prefabs (cross points etc.)
            // Distance between them are the roads
            // This way we don't have to walk through each road segment (which can be hundreds or thousands) each time we want to know the node-node length
            // This is a reduction of approximately 6x
            Dictionary<ulong, Dictionary<ulong, float>> cache = new Dictionary<ulong, Dictionary<ulong, float>>();

            foreach (var prefab in Items.Values.Where(x => x.HideUI == false && x.Type == Ets2ItemType.Prefab))
            {
                foreach (var node in prefab.NodesList.Values)
                {
                    var endNode = default(Ets2Item);

                    var fw = node.ForwardItem != null && node.ForwardItem.Type == Ets2ItemType.Road;
                    var road = node.ForwardItem != null && node.ForwardItem.Type == Ets2ItemType.Prefab
                    ? node.BackwardItem
                    : node.ForwardItem;
                    var totalLength = 0.0f;
                    var weight = 0.0f;
                    List<Ets2Item> roadList = new List<Ets2Item>();
                    while (road != null)
                    {
                        if (road.StartNode == null || road.EndNode == null)
                            break;
                        var length =
                            (float)Math.Sqrt(Math.Pow(road.StartNode.X - road.EndNode.X, 2) +
                                      Math.Pow(road.StartNode.Z - road.EndNode.Z, 2));
                        var spd = 1;
                        if (road.RoadLook != null)
                        {
                            if (road.RoadLook.IsExpress) spd = 25;
                            if (road.RoadLook.IsLocal) spd = 45;
                            if (road.RoadLook.IsHighway) spd = 70;
                        }

                        totalLength += length;
                        weight += length / spd;
                        roadList.Add(road);

                        if (fw)
                        {
                            road = road.EndNode == null?null: road.EndNode.ForwardItem;
                            if (road != null && road.Type == Ets2ItemType.Prefab)
                            {
                                endNode = road;
                                break;
                            }
                        }
                        else
                        {
                            road = road.StartNode == null ? null : road.StartNode.BackwardItem;
                            if (road != null && road.Type == Ets2ItemType.Prefab)
                            {
                                endNode = road;
                                break;
                            }
                        }
                    }

                    if (prefab.ItemUID == 0x002935DED8C0345C)
                    {
                        Console.WriteLine(node.NodeUID.ToString("X16") + " following to " + endNode.ItemUID.ToString("X16"));
                    }

                    // If there is no end-node found, it is a dead-end road.
                    if (endNode != null && prefab != endNode)
                    {
                        if (prefab.Navigation.ContainsKey(endNode) == false)
                        {
                            prefab.Navigation.Add(endNode,
                                new Tuple<float, float, IEnumerable<Ets2Item>>(weight, totalLength, roadList));
                        }
                        if (endNode.Navigation.ContainsKey(prefab) == false)
                        {
                            var reversedRoadList = new List<Ets2Item>(roadList);
                            reversedRoadList.Reverse();
                            endNode.Navigation.Add(prefab,
                                new Tuple<float, float, IEnumerable<Ets2Item>>(weight, totalLength, reversedRoadList));
                        }
                    }
                }
            }
        }

        public void Find(Ets2Node node, ulong item, bool isBackward)
        {
            var req = new Ets2ItemSearchRequest
            {
                ItemUID = item,
                Node = node,
                IsBackward = isBackward,
                IsForward = !isBackward
            };

            ItemSearchRequests.Add(req);
        }

        public string LookupCityID(ulong id)
        {
            return !CitiesLookup.ContainsKey(id) ? string.Empty : CitiesLookup[id];
        }

        public Ets2Prefab LookupPrefab(int prefabId)
        {
            if (PrefabLookup.ContainsKey(prefabId))
                return PrefabLookup[prefabId];
            else
                return null;
        }

        public Ets2RoadLook LookupRoadLookID(uint lookId)
        {
            if (RoadLookup.ContainsKey(lookId))
                return RoadLookup[lookId];
            else
                return null;
        }
    }
}