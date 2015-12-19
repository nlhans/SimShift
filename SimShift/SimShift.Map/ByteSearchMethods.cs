using System.Collections.Generic;

namespace SimShift.MapTool
{
    public static class ByteSearchMethods
    {
        public static unsafe List<int> IndexesOf(this byte[] Haystack, byte[] Needle)
        {
            List<int> Indexes = new List<int>();
            fixed (byte* H = Haystack)
            fixed (byte* N = Needle)
            {
                int i = 0;
                for (byte* hNext = H, hEnd = H + Haystack.Length; hNext < hEnd; i++, hNext++)
                {
                    bool Found = true;
                    for (byte* hInc = hNext, nInc = N, nEnd = N + Needle.LongLength;
                        Found && nInc < nEnd;
                        Found = *nInc == *hInc, nInc++, hInc++) ;
                    if (Found) Indexes.Add(i);
                }
                return Indexes;
            }
        }

        public static unsafe List<int> IndexesOfUlong(this byte[] Haystack, byte[] Needle)
        {
            List<int> Indexes = new List<int>();
            fixed (byte* H = Haystack)
            fixed (byte* N = Needle)
            {
                int i = 0;
                for (byte* hNext = H, hEnd = H + Haystack.Length; hNext < hEnd; i++, hNext++)
                {
                    if (*((ulong*) hNext) == *((ulong*) N)) Indexes.Add(i);
                }
                return Indexes;
            }
        }
    }
}