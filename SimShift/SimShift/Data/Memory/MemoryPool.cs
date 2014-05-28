using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
namespace SimTelemetry.Domain.Memory
{
    public class MemoryPool : IMemoryObject, IDataNode
    {
        public string Name { get; protected set; }
        public MemoryProvider Memory { get; set; }
        public MemoryAddress AddressType { get; protected set; }

        public bool IsTemplate { get; protected set; }

        public bool IsSignature { get { return Signature != string.Empty; } }
        public bool IsDynamic { get { return (AddressType == MemoryAddress.Dynamic); } }
        public bool IsStatic { get { return (AddressType == MemoryAddress.Static || AddressType == MemoryAddress.StaticAbsolute); } }
        public bool IsConstant { get { return false; } }

        public MemoryPool Pool { get; protected set; }
        public int Offset { get; protected set; }
        public int Address { get; protected set; }
        public int[] AddressTree { get; protected set; }
        public int Size { get; protected set; }
        public string Signature { get; protected set; }

        public byte[] Value { get; protected set; }
        public Type ValueType { get { return typeof(MemoryPool); } }

        Dictionary<string, IDataField> IDataNode.Fields { get { return _fields.Values.Cast<IDataField>().ToDictionary(x => x.Name, x => x); } }
        public Dictionary<string, IMemoryObject> Fields { get { return _fields; } }
        public Dictionary<string, MemoryPool> Pools { get { return _pools; } }

        private readonly Dictionary<string, IMemoryObject> _fields = new Dictionary<string, IMemoryObject>();
        private readonly Dictionary<string, MemoryPool> _pools = new Dictionary<string, MemoryPool>();

        public IEnumerable<MemoryFieldSignaturePointer> Pointers { get; protected set; }

        public TOut ReadAs<TOut>()
        {
            return MemoryDataConverter.Read<TOut>(new byte[32], 0);
        }

        public object Read()
        {
            return new byte[0];
        }

        public bool HasChanged()
        {
            return false;
        }

        public void MarkDirty()
        {
        }

        public TOut ReadAs<TOut>(int offset)
        {
            return MemoryDataConverter.Read<TOut>(Value, offset);
        }

        public TOut ReadAs<TSource, TOut>(int offset)
        {
            return MemoryDataConverter.Read<TSource, TOut>(Value, offset);
        }

        public TOut ReadAs<TOut>(string field)
        {
            if (Fields.ContainsKey(field))
                return Fields[field].ReadAs<TOut>();
            else
                return MemoryDataConverter.Read<TOut>(new byte[32], 0);
        }

        public IEnumerable<IDataField> GetDataFields()
        {
            return Fields.Select(x => (IDataField)x.Value);
        }

        public byte[] ReadBytes(string field)
        {
            var oField = this.Fields[field];
            if (oField.HasChanged())
            {
                return MemoryDataConverter.Rawify(oField.Read);
            }
            else
            {
                return new byte[0];
            }
        }

        public void GetDebugInfo(XmlWriter file)
        {
            file.WriteStartElement("debug");
            file.WriteAttributeString("name", this.Name);
            file.WriteAttributeString("fields", Fields.Count().ToString());

            file.WriteAttributeString("template", IsTemplate.ToString());
            if (AddressTree == null)
                file.WriteAttributeString("address", Address.ToString("X"));
            else
                file.WriteAttributeString("address", string.Concat(AddressTree.Select(x => x.ToString("X") + ", ")));
            file.WriteAttributeString("size", Size.ToString("X"));
            file.WriteAttributeString("offset", Offset.ToString("X"));

            foreach(var field in Fields)
            {
                file.WriteStartElement("debug-field");
                file.WriteAttributeString("name", field.Value.Name);
                file.WriteAttributeString("address", field.Value.Address.ToString());
                file.WriteAttributeString("size", field.Value.Size.ToString());
                file.WriteAttributeString("offset", field.Value.Offset.ToString());
                file.WriteAttributeString("type", field.Value.ValueType.ToString());
                file.WriteEndElement();
            }
            foreach(var pool in Pools)
            {
                pool.Value.GetDebugInfo(file);
            }

            file.WriteEndElement();

            //return string.Format("Type:{0}, Fields: {1}, IsTemplate: {2}, Address: 0x{3:X}, Offset: 0x{4:X}, Size: 0x{5:X}", AddressType, Fields.Count(), IsTemplate, Address, Offset, Size);
        }

        public void Refresh()
        {
            if (IsTemplate) return;

            var computedAddress = 0;

            if (IsSignature && Offset == 0 && Address == 0 && Memory.Scanner.Enabled)
            {
                var result = Memory.Scanner.Scan<uint>(MemoryRegionType.EXECUTE, Signature);

                // Search the address and offset.
                switch (AddressType)
                {
                    case MemoryAddress.StaticAbsolute:
                    case MemoryAddress.Static:
                        if (result == 0) return;

                        if (Pointers.Count() == 0)
                        {
                            // The result is directly our address
                            Address = (int)result;
                        }
                        else
                        {
                            // We must follow one pointer.
                            if (AddressType == MemoryAddress.Static)
                            {
                                computedAddress = Memory.BaseAddress + (int)result;
                            }
                            else
                            {
                                computedAddress = (int)result;
                            }

                            Address = computedAddress;
                        }
                        break;
                    case MemoryAddress.Dynamic:
                        Offset = (int)result;
                        break;
                    default:
                        throw new Exception("AddressType for '" + Name + "' is not valid");
                        break;
                }
            }

            // Refresh pointers too
            foreach (var ptr in Pointers)
                ptr.Refresh(Memory);

            // Refresh this memory block.
            if (Size > 0)
            {
                AddressTree = new int[1+Pointers.Count()];
                if (IsStatic)
                {
                    if (Address != 0 && Offset != 0)
                    {
                        computedAddress = Memory.Reader.ReadInt32(Memory.BaseAddress + Address) + Offset;
                    }
                    else
                    {
                        computedAddress = AddressType == MemoryAddress.Static
                                              ? Memory.BaseAddress + Address
                                              : Address;
                    }
                }
                else
                {
                    computedAddress = Pool == null ? 0 : MemoryDataConverter.Read<int>(Pool.Value, Offset);
                }
                int treeInd = 0;
                foreach (var ptr in Pointers)
                {
                    AddressTree[treeInd++] = computedAddress;
                    if (ptr.Additive)
                        computedAddress += ptr.Offset;
                    else
                        computedAddress = Memory.Reader.ReadInt32(computedAddress + ptr.Offset);
                }
                AddressTree[treeInd] = computedAddress;

                // Read into this buffer.
                Memory.Reader.Read(computedAddress, Value);
            }

            // Refresh underlying fields.
            foreach (var field in Fields)
                field.Value.Refresh();

            foreach (var pool in Pools.Values)
                pool.Refresh();

        }

        public void SetProvider(MemoryProvider provider)
        {
            Memory = provider;
            foreach (var field in _fields) field.Value.SetProvider(provider);
            foreach (var pool in _pools) pool.Value.SetProvider(provider);
        }

        public void SetPool(MemoryPool pool)
        {
            if (Pool == pool) return;
            Pool = pool;
        }

        public void Add<T>(T obj) where T : IMemoryObject
        {
            if (typeof(T).Name.Contains("MemoryPool")) throw new Exception();
            if (!_fields.ContainsKey(obj.Name))
            {
                _fields.Add(obj.Name, obj);

                obj.SetPool(this);
                if (Memory != null) obj.SetProvider(Memory);
            }
        }

        public void Add(MemoryPool obj)
        {
            if (!_pools.ContainsKey(obj.Name))
            {
                _pools.Add(obj.Name, obj);

                obj.SetPool(this);
                if (Memory != null) obj.SetProvider(Memory);
            }
        }

        public void ClearPools()
        {
            _pools.Clear();
        }

        public MemoryPool(string name, MemoryAddress type, string signature, int size)
        {
            Name = name;
            Address = 0;
            Offset = 0;
            Size = size;
            AddressType = type;
            Signature = signature;
            Pointers = new List<MemoryFieldSignaturePointer>();

            Value = new byte[size];
        }


        public MemoryPool(string name, MemoryAddress type, string signature, IEnumerable<int> pointers, int size)
        {
            Name = name;
            Address = 0;
            Offset = 0;
            Size = size;
            AddressType = type;
            Signature = signature;
            Pointers = pointers.Select(pointer => new MemoryFieldSignaturePointer(pointer, false)).ToList();

            Value = new byte[size];
        }

        public MemoryPool(string name, MemoryAddress type, string signature, IEnumerable<MemoryFieldSignaturePointer> pointers, int size)
        {
            Name = name;
            Address = 0;
            Offset = 0;
            Size = size;
            AddressType = type;
            Signature = signature;
            Pointers = pointers;

            Value = new byte[size];
        }

        public MemoryPool(string name, MemoryAddress type, int address, IEnumerable<int> pointers, int size)
        {
            Name = name;
            Address = address;
            Offset = 0;
            Size = size;
            AddressType = type;
            Signature = string.Empty;
            Pointers = pointers.Select(pointer => new MemoryFieldSignaturePointer(pointer, false)).ToList();

            Value = new byte[Size];
        }

        public MemoryPool(string name, MemoryAddress type, int address, IEnumerable<MemoryFieldSignaturePointer> pointers, int size)
        {
            Name = name;
            Address = address;
            Offset = 0;
            Size = size;
            AddressType = type;
            Signature = string.Empty;
            Pointers = pointers;

            Value = new byte[Size];
        }

        public MemoryPool(string name, MemoryAddress type, int address, int size)
        {
            Name = name;
            Address = address;
            Offset = 0;
            Size = size;
            AddressType = type;
            Signature = string.Empty;
            Pointers = new List<MemoryFieldSignaturePointer>();

            Value = new byte[Size];
        }

        public MemoryPool(string name,  MemoryAddress type, int address, int offset, int size)
        {
            Name = name;
            Address = address;
            Offset = offset;
            Size = size;
            AddressType = type;
            Signature = string.Empty;
            Pointers = new List<MemoryFieldSignaturePointer>();

            Value = new byte[Size];
        }

        public MemoryPool(string name,  MemoryAddress type, MemoryPool pool, int offset, int size)
        {
            Name = name;
            Pool = pool;
            Offset = offset;
            Size = size;
            AddressType = type;
            Signature = string.Empty;
            Pointers = new List<MemoryFieldSignaturePointer>();

            Value = new byte[Size];
        }


        public void SetTemplate(bool yes)
        {
            IsTemplate = yes;
        }

        public object Clone()
        {
            // cannot clone without arguments.
            return null;
        }

        public MemoryPool Clone(string newName, MemoryPool newPool, int offset, int size)
        {
            var target = new MemoryPool(newName, AddressType, newPool, offset, size);
            CloneContents(target);
            return target;
        }


        public IDataNode Clone(string newName, int address)
        {
            var target = new MemoryPool(newName, AddressType, address, Size);
            CloneContents(target);
            return (IDataNode)target;
        }
        protected void CloneContents(MemoryPool target)
        {
            foreach (var pool in Pools)
                target.Add(pool.Value.Clone(pool.Key, target, pool.Value.Offset, pool.Value.Size));

            foreach (var field in Fields)
                target.Add((IMemoryObject)field.Value.Clone());
        }

    }
}