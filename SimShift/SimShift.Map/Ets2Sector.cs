using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimShift.MapTool
{
    public class Ets2Sector
    {
        public string FilePath { get; private set; }
        public Ets2Mapper Mapper { get; private set; }

        // Status flags:
        public bool Empty { get; private set; }
        public bool NoFooterError { get; private set; }

        public List<Ets2Node> Nodes { get; private set; }
        public List<Ets2Item> Items { get; private set; }

        private int FooterStart { get; set; }
        public byte[] Stream { get; private set; }

        private Dictionary<int, int> FileMap = new Dictionary<int, int>();

        public Ets2Sector(Ets2Mapper mapper, string file)
        {
            Mapper = mapper;
            FilePath = file;

            Nodes = new List<Ets2Node>();
            Items = new List<Ets2Item>();
            FooterStart = -1;

            Stream = File.ReadAllBytes(file);
            Empty = Stream.Length < 60;
        }

        public void ParseNodes()
        {
            if (Empty) return;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // First determine the number of positions in this file
            var nodesPieces = BitConverter.ToInt32(Stream, 0x10);

            int i = Stream.Length;

            do
            {
                i -= 56; // each block is 56 bytes long

                var node = new Ets2Node(Stream, i);
                if (node.NodeUID == 0)
                {
                    FooterStart = i + 56 - 4;
                    break;
                }
                Nodes.Add(node);

                if (Mapper.Nodes.ContainsKey(node.NodeUID) == false)
                    Mapper.Nodes.TryAdd(node.NodeUID, node);

                var count = BitConverter.ToInt32(Stream, i - 4);
                if (count >= nodesPieces && count == Nodes.Count)
                {
                    FooterStart = i - 4;
                    break;
                }

            } while (i > 60);
            //Console.WriteLine("Thereby footer starts at " + FooterStart.ToString("X16"));
            if (FooterStart < 0)
            {
                NoFooterError = true;
                return;
            }

            sw.Stop();
            //Console.WriteLine(Path.GetFileNameWithoutExtension(FilePath) + " contains " + Nodes.Count +
            //                  " nodes; parsed in " + sw.ElapsedMilliseconds + "ms");
        }

        public void ParseItems()
        {
            if (Empty) return;
            if (NoFooterError) return;

            foreach (var node in Nodes)
            {
                FindItems(node, true);
            }
        }

        private IEnumerable<int> SearchUID(byte[] uid)
        {
            return Stream.IndexesOfUlong(uid);
            byte firstMatchByte = uid[0];
            for (int i = 0; i < FooterStart - 8; i++)
            {
                if (firstMatchByte == Stream[i])
                {
                    byte[] match = new byte[8];
                    Array.Copy(Stream, i, match, 0, 8);
                    if (match.SequenceEqual<byte>(uid))
                    {
                        //yield return i;
                        i += 8;
                    }
                }
            }
        }

        public Ets2Item FindItem(ulong uid)
        {
            Tuple<string, int> inp;
            if (uid == 0)
                return null;
            /*if (Mapper.ItemCache.TryGetValue(uid, out inp))
            {
                var item = new Ets2Item(uid,
                    Mapper.Sectors.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.FilePath) == inp.Item1),
                    inp.Item2);
                if (!item.Valid)
                {
                    lock (Mapper.ItemCache)
                    {
                        Mapper.ItemCache.Remove(uid);
                    }
                }
                else
                {
                    return item;
                }
            }*/
            var pos = SearchUID(BitConverter.GetBytes(uid));

            foreach (var off in pos)
            {
                var offs = off - 4;

                var type = BitConverter.ToUInt32(Stream, offs);

                if (type < 0x40 && type != 0)
                {
                    var item = new Ets2Item(uid, this, offs);
                    if (item.Valid)
                    {
                        /*lock (Mapper.ItemCache)
                        {
                            Mapper.ItemCache.Add(uid,
                                new Tuple<string, int>(Path.GetFileNameWithoutExtension(FilePath), offs));
                        }*/

                        return item;
                    }
                }
            }

            return null;
        }

        public void FindItems(Ets2Node node, bool postpone)
        {
            if (node.ForwardItemUID > 0)
            {
                Ets2Item itemForward;
                if (Mapper.Items.TryGetValue(node.ForwardItemUID, out itemForward))
                {
                    if (itemForward.Apply(node))
                        node.ForwardItem = itemForward;
                }
                else
                {
                    itemForward = FindItem(node.ForwardItemUID);
                    if (itemForward == null)
                    {
                        if (postpone)
                            Mapper.Find(node, node.ForwardItemUID, false);
                    }
                    else
                    {
                        Items.Add(itemForward);
                        Mapper.Items.TryAdd(node.ForwardItemUID, itemForward);
                        node.ForwardItem = itemForward;

                        if (itemForward.Apply(node))
                            node.ForwardItem = itemForward;
                    }
                }
            }
            if (node.BackwardItemUID > 0)
            {
                Ets2Item itemBackward;
                if (Mapper.Items.TryGetValue(node.BackwardItemUID, out itemBackward))
                {
                    if (itemBackward.Apply(node))
                        node.BackwardItem = itemBackward;
                }
                else
                {
                    itemBackward = FindItem(node.BackwardItemUID);
                    if (itemBackward == null)
                    {
                        if (postpone)
                            Mapper.Find(node, node.BackwardItemUID, true);
                    }
                    else
                    {
                        Items.Add(itemBackward);
                        Mapper.Items.TryAdd(node.BackwardItemUID, itemBackward);
                        node.BackwardItem = itemBackward;

                        if (itemBackward.Apply(node))
                            node.BackwardItem = itemBackward;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Sector " + Path.GetFileNameWithoutExtension(FilePath);
        }
    }
}