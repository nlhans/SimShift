using System;
using System.Collections.Generic;
using System.Linq;

namespace SimShift.Utils
{
    public class IniValueObject
    {
        public IEnumerable<string> NestedGroup { get; private set; }
        public string Group { get { return NestedGroup.ElementAt(NestedGroup.Count() - 1); } }
        public string NestedGroupName { get { return string.Join(".", NestedGroup); } }

        public string Key { get; private set; }
        public string RawValue { get; private set; }

        protected string Value { get; private set; }
        protected string[] ValueArray { get; private set; }
        public bool IsTuple { get; private set; }

        public IniValueObject(IEnumerable<string> nestedGroup, string key, string rawValue)
        {
            NestedGroup = nestedGroup;
            Key = key;
            RawValue = rawValue;

            var value = rawValue;

            // Does this rawValue contain multiple values?
            if (value.StartsWith("(") && value.EndsWith(")") && value.Length > 2)
                value = value.Substring(1, value.Length - 2);
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 2)
                value = value.Substring(1, value.Length - 2);

            if (value.Contains(","))
            {
                IsTuple = true;

                var values = value.Split(new[] { ',' });
                ValueArray = new string[values.Length];

                for (var i = 0; i < values.Length; i++)
                {
                    var val = values[i];
                    if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length > 2)
                        val = val.Substring(1, val.Length - 2);

                    ValueArray[i] = val.Trim();

                }
            }
            else
            {

                IsTuple = false;
                Value = value;
            }
        }

        public bool BelongsTo(string group)
        {
            return NestedGroup.Contains(group);
        }

        public int ReadAsInteger(int index)
        {
            if (!IsTuple) throw new Exception("This is not a tuple value");
            return int.Parse(ValueArray[index]);
        }

        public double ReadAsDouble(int index)
        {
            if (!IsTuple) throw new Exception("This is not a tuple value");
            return double.Parse(ValueArray[index]);
        }

        public float ReadAsFloat(int index)
        {
            if (!IsTuple) throw new Exception("This is not a tuple value");
            return float.Parse(ValueArray[index]);
        }

        public string ReadAsString(int index)
        {
            if (!IsTuple) throw new Exception("This is not a tuple value");
            return ValueArray[index];
        }

        public string ReadAsString()
        {
            return IsTuple ? ReadAsString(0) : Value;
        }

        public int ReadAsInteger()
        {
            return IsTuple ? ReadAsInteger(0) : int.Parse(Value);
        }

        public double ReadAsDouble()
        {
            return IsTuple ? ReadAsDouble(0) : double.Parse(Value);
        }


        public float ReadAsFloat()
        {
            return IsTuple ? ReadAsFloat(0) : float.Parse(Value);
        }

        public IEnumerable<string> ReadAsStringArray()
        {
            return IsTuple ? ValueArray : new string[] { Value };
        }
    }
}