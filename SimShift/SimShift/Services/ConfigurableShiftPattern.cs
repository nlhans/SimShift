namespace SimShift.Services
{
    public struct ConfigurableShiftPattern
    {
        public string Region { get; private set; }
        public string File { get; private set; }

        public ConfigurableShiftPattern(string region, string file) : this()
        {
            Region = region;
            File = file;
        }
    }
}