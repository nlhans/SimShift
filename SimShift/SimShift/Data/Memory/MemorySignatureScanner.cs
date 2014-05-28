using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimTelemetry.Domain.Memory
{
    public class MemorySignatureScanner : IDisposable
    {
        protected class MemorySignatureScanObject
        {
            public byte data;
            public bool wildcard;
            public bool target;

            public MemorySignatureScanObject(byte data, bool wildcard, bool target)
            {
                this.data = data;
                this.wildcard = wildcard;
                this.target = target;
            }
        }

        public MemoryReader Reader { get; protected set; }
        public bool Enabled { get; protected set; }

        public MemorySignatureScanner(MemoryReader mem)
        {
            Reader = mem;
        }
        public MemorySignatureScanner(MemoryProvider provider)
        {
            Reader = provider.Reader;
        }

        public void Enable(string file)
        {
            byte[] d = File.ReadAllBytes(file);
            foreach(var region in Reader.Regions)
            {
                region.Data = d;
                region.Size = d.Length;
                break;
            }

            Enabled = true;
        }

        public void Enable()
        {
            // Get all regions from MemoryReader, and fill them with data.
            foreach(var region in Reader.Regions)
            {
                if (region.Size > 0x1000000) continue;
                region.Data = new byte[region.Size];
                Reader.Read(region.BaseAddress, region.Data);
            }

            Enabled = true;

        }

        public void Disable()
        {
            // Clear memory data.
            foreach(var region in Reader.Regions)
            {
                region.Data = new byte[0];
            }
            Enabled = false;
        }

        public void Dispose()
        {
            if (Enabled)
                Disable();
        }

        protected virtual IEnumerable<MemorySignatureScanObject> ParseSignature(string signature)
        {
            if (signature.Length % 2 != 0)
                throw new Exception("Not valid signature");

            var signatureObjects = new List<MemorySignatureScanObject>();

            for (var i = 0; i < signature.Length; i += 2)
            {
                var sigHex = signature.Substring(i, 2);

                if (sigHex.Contains("X") && sigHex != "XX") throw new Exception("Signature error at index " + i);
                if (sigHex.Contains("?") && sigHex != "??") throw new Exception("Signature error at index " + i);

                MemorySignatureScanObject signatureObject;
                switch (sigHex)
                {
                    case "XX":
                        if (i == 0) throw new Exception("Cannot start signature with a wildcard.");
                        signatureObject = new MemorySignatureScanObject(00, true, false);
                        break;
                    case "??":
                        if (i == 0) throw new Exception("Cannot start signature with a target address.");
                        signatureObject = new MemorySignatureScanObject(00, false, true);
                        break;
                    default:
                        signatureObject = new MemorySignatureScanObject((byte) Convert.ToUInt32(sigHex, 16), false, false);
                        break;
                }
                signatureObjects.Add(signatureObject);
            }
            return signatureObjects;
        }

        public T Scan<T>(MemoryRegionType memoryRegionType, string signature)
        {
            var results = ScanMemory<T>(memoryRegionType, signature);
            if (results != null)
            {
                // Get the best match out of it.
                var best = (from entry in results orderby entry.Value descending select entry.Key).FirstOrDefault();
                return best;
            }
            else
                return new List<T>().FirstOrDefault();
        }

        public IEnumerable<T> ScanAll<T>(MemoryRegionType memoryRegionType, string signature)
        {
            var results = ScanMemory<T>(memoryRegionType, signature);
            if (results == null)
                return new List<T>();
            else
                return results.Keys;
        }

        public Dictionary<T, int> ScanAllFrequencies<T>(MemoryRegionType memoryRegionType, string signature)
        {
            var results = ScanMemory<T>(memoryRegionType, signature);
            if (results == null)
                return new Dictionary<T, int>();
            else
                return results;
        }

        protected Dictionary<T, int> ScanMemory<T>(MemoryRegionType memoryRegionType, string signature)
        {
            if (!Enabled)
                return null;

            var signatureObject = ParseSignature(signature);

            var results = new Dictionary<T, int>();

            foreach(var region in Reader.Regions.Where(x => x.MatchesType(memoryRegionType)))
            {
                //for (int i = 0; i < 100; i++)
                    ScanRegion<T>(region, signatureObject, (value, address) =>
                                                               {
                                                                   if (results.ContainsKey(value))
                                                                       results[value]++;
                                                                   else
                                                                       results.Add(value, 1);
                                                               });
            }

            return results;
        }

        private void ScanRegion<T>(MemoryRegion region, IEnumerable<MemorySignatureScanObject> signature, Action<T, int> addAction)
        {
            if (region.Data.Length == 0 )
                return;

            byte startByte = signature.FirstOrDefault().data;
            var signatureLength = signature.Count();

            var address = (uint) Array.IndexOf(region.Data, startByte, 0);

            var signatureCheckIndex = 0;
            var targetIndex = 0;
            var target = new byte[32];
            var matchFailed = false;

            while (address < region.Data.Length-signatureLength)
            {

                foreach (var sigByte in signature)
                {
                    if (sigByte.wildcard)
                    {
                        signatureCheckIndex++;
                        continue;
                    }
                    var by = region.Data[address + signatureCheckIndex];

                    if (sigByte.target)
                    {
                        target[targetIndex++] = by;
                    }
                    else if (sigByte.data != by)
                    {
                        matchFailed = true;
                        break;
                    }

                    signatureCheckIndex++;
                }
                if (!matchFailed)
                {
                    addAction(MemoryDataConverter.Read<T>(target, 0), (int) address);
                    //Debug.WriteLine("0x{0} -> 0x{2:X} [ 0x{1:X} ] ",string.Format("{0:X}", address), BitConverter.ToString(target), address);

                    target = new byte[32];
                }
                signatureCheckIndex = 0;
                targetIndex = 0;
                matchFailed = false;

                address = (uint)Array.IndexOf(region.Data, startByte, (int)address + 1);
            }

        }
    }
}