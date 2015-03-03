using System.Collections.Generic;
using SimShift.Utils;

namespace SimShift.Entities
{
    public interface IConfigurable
    {
        IEnumerable<string> AcceptsConfigs { get; } 

        void ResetParameters();
        void ApplyParameter(IniValueObject obj);
        IEnumerable<IniValueObject> ExportParameters();
    }
}