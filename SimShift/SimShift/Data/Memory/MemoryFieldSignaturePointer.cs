namespace SimTelemetry.Domain.Memory
{
    public class MemoryFieldSignaturePointer
    {
        public int Offset { get; private set; }
        public string Signature { get; private set; }
        public bool IsDirty { get; private set; }
        public bool Additive { get; private set; }

        
        public MemoryFieldSignaturePointer(string signature, bool additive)
        {
            Signature = signature;
            Additive = additive;
            MarkDirty();
        }

        public MemoryFieldSignaturePointer(int offset, bool additive)
        {
            Offset = offset;
            Additive = additive;
            IsDirty = false;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void Refresh(MemoryProvider master)
        {
            if (IsDirty && master.Scanner.Enabled && Signature != string.Empty)
            {
                Offset = master.Scanner.Scan<int>(MemoryRegionType.EXECUTE, Signature);
                IsDirty = false;
            }
        }
    }
}