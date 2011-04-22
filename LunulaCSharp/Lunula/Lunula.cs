// Lunula
// A self-applicable Scheme to C# compiler
//
// Copyright (c) Anthony Fairchild
//
// This software is subject to the Microsoft Public License
// (Ms-PL). See LICENSE.txt for details.
//
//#define PROFILER

using System;
using System.Collections.Generic;
using System.IO;

namespace Lunula {
    public class Void {
        Void() { }
        public static Void TheVoidValue = new Void();
    }

    public class Symbol : object {
        static readonly Dictionary<string, Symbol> InternedSymbols = new Dictionary<string, Symbol>();
        readonly string _name;
        public string Name { get { return _name; } }
        Symbol(string name) { _name = name; }
        public static Symbol Intern(string name) {
            Symbol value;
            if (InternedSymbols.TryGetValue(name, out value)) {
                return value;
            }
            value = new Symbol(name);
            InternedSymbols[name] = value;
            return value;
        }
        public override string ToString() { return "SYM:" + _name; }
        public override int GetHashCode() {
            return _name.GetHashCode();
        }
    }

    class PeekableTextReaderAdapter {
        readonly TextReader _reader;
        readonly List<int> _queue = new List<int>();

        public PeekableTextReaderAdapter(TextReader reader) {
            _reader = reader;
        }

        public void Close() {
            _reader.Close();
        }

        public int Peek(int n) {
            while (_queue.Count < (n + 1)) {
                _queue.Add(_reader.Read());
            }
            return _queue[n];
        }
        public int Read() {
            if (_queue.Count > 0) {
                var item = _queue[0];
                _queue.RemoveAt(0);
                return item;
            }
            return _reader.Read();
        }
    }

    public class TaggedType {
        readonly Symbol _tag;
        public object Data;
        public TaggedType(Symbol tag, object data) {
            _tag = tag;
            Data = data;
        }
        public Symbol Tag { get { return _tag; } }

    }

    public class LexicalEnvironment {
        public LexicalEnvironment Parent;
        public object[] Bindings;
        public LexicalEnvironment(LexicalEnvironment parent, int size) {
            Parent = parent;
            Bindings = new object[size];
        }
    }

    public class LunulaException : Exception {
        public LunulaException(string msg) : base(msg) { }
    }

    public class SymbolNotFoundException : LunulaException {
        public SymbolNotFoundException(Symbol s) : base(string.Format("Symbol {0} is not defined.", s.Name)) { }
    }

    public class EOF { public static EOF TheEOFValue = new EOF(); }
}