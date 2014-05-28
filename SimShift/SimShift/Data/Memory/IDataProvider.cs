using System.Collections.Generic;

namespace SimTelemetry.Domain.Memory
{
    public interface IDataProvider
    {
        IDataNode Get(string name);
        IEnumerable<IDataNode> GetAll();

        void MarkDirty();
        void Refresh();
    }
}