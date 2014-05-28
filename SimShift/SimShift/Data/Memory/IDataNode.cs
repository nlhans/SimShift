using System.Collections.Generic;
using System.Xml;

namespace SimTelemetry.Domain.Memory
{
    public interface IDataNode
    {
        Dictionary<string, IDataField> Fields { get; }

        string Name { get; }
        T ReadAs<T>(string field);
        IEnumerable<IDataField> GetDataFields();
        byte[] ReadBytes(string field);
        void GetDebugInfo(XmlWriter file);
        IDataNode Clone(string newName, int newAddress);
    }
}