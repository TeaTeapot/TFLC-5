using System;
using System.Collections.Generic;

namespace TextEditor
{
    public class SymbolEntry
    {
        public string Name { get; set; }
        public string Type { get; set; }        
        public string Value { get; set; }       
        public int DeclaredLine { get; set; }
        public int DeclaredColumn { get; set; }
        public bool IsUsed { get; set; } = false;
    }

    public class SymbolTable
    {
        private readonly Dictionary<string, SymbolEntry> _table
            = new Dictionary<string, SymbolEntry>(StringComparer.Ordinal);

        public bool Declare(string name, string type,
                            string value = null, int line = -1, int col = -1)
        {
            if (_table.ContainsKey(name))
                return false;
            _table[name] = new SymbolEntry
            {
                Name = name,
                Type = type,
                Value = value,
                DeclaredLine = line,
                DeclaredColumn = col
            };
            return true;
        }

        public SymbolEntry Lookup(string name)
        {
            _table.TryGetValue(name, out var entry);
            return entry;
        }

        public void UpdateType(string name, string type)
        {
            if (_table.TryGetValue(name, out var entry))
                entry.Type = type;
        }

        public void MarkUsed(string name)
        {
            if (_table.TryGetValue(name, out var entry))
                entry.IsUsed = true;
        }

        public IEnumerable<SymbolEntry> All() => _table.Values;

        public void Clear() => _table.Clear();
    }
}
