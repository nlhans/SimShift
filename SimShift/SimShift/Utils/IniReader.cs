using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimShift.Utils
{
    public class IniReader : IDisposable
    {
        public string Filedata { get; private set; }
        public string Filename { get; private set; }

        protected readonly IList<Action<IniValueObject>> _handlers = new List<Action<IniValueObject>>();
        protected readonly IList<string> _group = new List<string>();

        public IniReader(string dataSource)
            : this(dataSource, true)
        {

        }

        public IniReader(string dataSource, bool isFileData)
        {
            Filedata = string.Empty;

            if (isFileData)
            {
                if (File.Exists(dataSource) == false)
                    throw new IOException("Could not find file " + dataSource);
                Filename = dataSource;
                Filedata = File.ReadAllText(dataSource);
            }
            else
            {
                Filedata = dataSource;
            }

        }

        public void AddHandler(Action<IniValueObject> handler)
        {
            if (_handlers.Contains(handler) == false)
                _handlers.Add(handler);
        }

        public void RemoveHandler(Action<IniValueObject> handler)
        {
            if (_handlers.Contains(handler))
                _handlers.Remove(handler);
        }

        public void Parse()
        {
            if (Filedata == string.Empty)
                throw new Exception("No data assigned to this reader");

            var filelines = Filedata
                .Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Contains("//") ? x.Remove(x.IndexOf("//")).Trim() : x.Trim())
                .Where(x => x.Length != 0)
                .ToList();

            ApplyGroup("Main", false);

            for (var i = 0; i < filelines.Count; i++)
            {
                var line = filelines[i];
                var nextLine = (i + 1 == filelines.Count) ? "" : filelines[i + 1];

                if (line == "{") continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (line.Length < 3)
                        ApplyGroup("", false);
                    ApplyGroup(line.Substring(1, line.Length - 2), false);
                    continue;
                }

                if (line == "}")
                {
                    LeaveGroup(true);
                    continue;
                }

                if (nextLine == "{")
                {
                    // This is a header.
                    ApplyGroup(line, true);
                    continue;
                }

                // Parse this value.
                var key = "";
                var value = "";

                if (line.Contains("="))
                {
                    var data = line.Split(new[] { '=' }, 2);
                    key = data[0].Trim();
                    value = data[1].Trim();
                }
                else
                {
                    value = line;
                }

                var obj = new IniValueObject(_group, key, value);

                foreach (var handler in _handlers)
                    handler(obj);

            }
        }

        private void LeaveGroup(bool nest)
        {
            if (nest == false || _group.Count <= 1)
            {
                ApplyGroup("Main", false);
            }
            else
            {
                _group.RemoveAt(_group.Count - 1); // remove last element
            }
        }

        public void ApplyGroup(string group, bool nest)
        {
            if (nest == false)
            {
                _group.Clear();
            }

            _group.Add(group);
        }

        public void Dispose()
        {
            Filedata = null;
            _group.Clear();
            _handlers.Clear();
        }
    }
}
