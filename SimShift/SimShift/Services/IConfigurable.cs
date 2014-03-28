using System.Collections.Generic;
using SimShift.Utils;

namespace SimShift.Services
{
    public interface IConfigurable
    {
        IEnumerable<string> AcceptsConfigs { get; } 

        void ResetParameters();
        void ApplyParameter(IniValueObject obj);
        IEnumerable<IniValueObject> ExportParameters();
    }
}