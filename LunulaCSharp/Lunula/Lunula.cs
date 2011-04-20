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
using System.Diagnostics;
using System.Linq;


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

    public class Cons {
        object _car;
        object _cdr;
        public Cons(object car, object cdr) {
            _car = car;
            _cdr = cdr;
        }
        public static object Car(object cons) {
            if (cons is Cons) return ((Cons)cons)._car;
            if (cons == null) return cons;
            throw new LunulaException(string.Format("object {0} is not a cons", cons));
        }
        public static object Cdr(object cons) {
            if (cons is Cons) return ((Cons)cons)._cdr;
            if (cons == null) return cons;
            throw new LunulaException(string.Format("object {0} is not a cons", cons));
        }
        public static object SetCar(object c, object v) {
            if (c is Cons) ((Cons)c)._car = v;
            else throw new LunulaException(string.Format("object {0} is not a cons", c));
            return Void.TheVoidValue;
        }
        public static object SetCdr(object c, object v) {
            if (c is Cons) ((Cons)c)._cdr = v;
            else throw new LunulaException(string.Format("object {0} is not a cons", c));
            return Void.TheVoidValue;
        }
        public static object Reverse(object cons) {
            object reversed = null;
            while (cons != null) {
                reversed = new Cons(Car(cons), reversed);
                cons = Cdr(cons);
            }
            return reversed;
        }
        public static int Length(object cons) {
            int length = 0;
            while (cons != null) {
                length++;
                cons = Cdr(cons);
            }
            return length;
        }

        public static object[] ToReverseObjectArray(object cons) {
            var length = Length(cons);
            var objs = new object[length];
            for (var x = 0; x < length; x++) {
                objs[length - 1 - x] = Car(cons);
                cons = Cdr(cons);
            }
            return objs;
        }
        public static object[] ToObjectArray(object cons) {
            var length = Length(cons);
            var objs = new object[length];
            for (var x = 0; x < length; x++) {
                objs[x] = Car(cons);
                cons = Cdr(cons);
            }
            return objs;
        }
        public static object ConsFromIEnumerable(IEnumerable<object> collection) {
            var c = collection.Aggregate<object, object>(null, (current, thing) => new Cons(thing, current));
            return Reverse(c);
        }
        public static object ConsFromArray(object[] array) {
            var c = array.Aggregate<object, object>(null, (current, t) => new Cons(t, current));
            return Reverse(c);
        }
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

    public class Builtins {
        readonly LunulaVM _vm;

        public Builtins(LunulaVM vm) {
            _vm = vm;
            vm.DefineFunction("@make-vector", MakeVector);
            vm.DefineFunction("@vector-ref", VectorRef);
            vm.DefineFunction("@vector-set", VectorSet);
            vm.DefineFunction("@vector?", IsVector);
            vm.DefineFunction("@vector-length", VectorLength);
            vm.DefineFunction("@make-hash-table", MakeHashTable);
            vm.DefineFunction("@hash-ref", HashRef);
            vm.DefineFunction("@hash-set!", HashSet);
            vm.DefineFunction("@hash-remove!", HashRemove);
            vm.DefineFunction("@hash-keys", HashKeys);
            vm.DefineFunction("@hash-values", HashValues);
            vm.DefineFunction("@boolean?", IsBoolean);
            vm.DefineFunction("@boolean=?", BooleanEquals);
            vm.DefineFunction("@char?", IsChar);
            vm.DefineFunction("@char=?", CharEquals);
            vm.DefineFunction("@char-alphabetic?", IsCharAlphabetic);
            vm.DefineFunction("@char-numeric?", IsCharNumeric);
            vm.DefineFunction("@char-code", CharCode);
            vm.DefineFunction("@symbol?", IsSymbol);
            vm.DefineFunction("@symbol=?", SymbolEquals);
            vm.DefineFunction("@void?", IsVoid);
            vm.DefineFunction("@void", MakeVoid);
            vm.DefineFunction("@procedure?", IsProcedure);
            vm.DefineFunction("@toplevel-defined?", ToplevelIsDefined);
            vm.DefineFunction("@toplevel-define", ToplevelDefine);
            vm.DefineFunction("@toplevel-lookup", ToplevelLookup);
            vm.DefineFunction("cons?", IsCons);
            vm.DefineFunction("null?", IsNull);
            vm.DefineFunction("car", Car);
            vm.DefineFunction("cdr", Cdr);
            vm.DefineFunction("@string?", IsString);
            vm.DefineFunction("@string=?", StringEquals);
            vm.DefineFunction("@string-length", StringLength);
            vm.DefineFunction("@string-append", StringAppend);
            vm.DefineFunction("@number->string", NumberToString);
            vm.DefineFunction("@symbol->string", SymbolToString);
            vm.DefineFunction("@open-output-string", OpenOutputString);
            vm.DefineFunction("@open-input-string", OpenInputString);
            vm.DefineFunction("@get-output-string", GetOutputString);
            vm.DefineFunction("@open-output-byte-array", OpenOutputByteArray);
            vm.DefineFunction("@get-output-byte-array", GetOutputByteArray);
            vm.DefineFunction("@write-byte", WriteByte);
            vm.DefineFunction("@write-word", WriteWord);
            vm.DefineFunction("@write-dword", WriteDWord);
            vm.DefineFunction("@current-output-port", GetStdOut);
            vm.DefineFunction("@current-error-port", GetStdError);
            vm.DefineFunction("@current-input-port", GetStdIn);
            vm.DefineFunction("@cons", MakeCons);
            vm.DefineFunction("@set-car!", SetCar);
            vm.DefineFunction("@set-cdr!", SetCdr);
            vm.DefineFunction("@number?", IsNumber);
            vm.DefineFunction("@=", NumberEqual);
            vm.DefineFunction("@>", NumberGreaterThan);
            vm.DefineFunction("@>=", NumberGreaterThanAndEqual);
            vm.DefineFunction("@<", NumberLessThan);
            vm.DefineFunction("@<=", NumberLessThanAndEqual);
            vm.DefineFunction("@+", TwoArgPlus);
            vm.DefineFunction("@-", TwoArgMinus);
            vm.DefineFunction("@*", TwoArgMultiply);
            vm.DefineFunction("@/", TwoArgDivide);
            vm.DefineFunction("@bor", BinaryOr);
            vm.DefineFunction("@left-shift", LeftShift);
            vm.DefineFunction("@write-char", WriteChar);
            vm.DefineFunction("@write-string", WriteString);
            vm.DefineFunction("@flush-output", FlushOutput);
            vm.DefineFunction("@to-string", StaticToString);
            vm.DefineFunction("@open-file-output-port", OpenFileOutputPort);
            vm.DefineFunction("@open-binary-file-output-port", OpenBinaryFileOutputPort);
            vm.DefineFunction("@close-output-port", CloseOutputPort);
            vm.DefineFunction("@open-file-input-port", OpenFileInputPort);
            vm.DefineFunction("@close-input-port", CloseInputPort);
            vm.DefineFunction("@get-time", GetTime);
            vm.DefineFunction("@time-difference", TimeDifference);
            vm.DefineFunction("@string->list", StringToList);
            vm.DefineFunction("@list->string", ListToString);
            vm.DefineFunction("@string->number", StringToNumber);
            vm.DefineFunction("@string->symbol", StringToSymbol);
            vm.DefineFunction("@fail", Fail);
            vm.DefineFunction("@read-char", ReadChar);
            vm.DefineFunction("@peek-char", PeekChar);
            vm.DefineFunction("@peek-char-skip", PeekCharSkip);
            vm.DefineFunction("@eof-object?", IsEOFObject);
            vm.DefineFunction("@cons?", thing => thing is Cons);
            vm.DefineFunction("@null?", thing => thing == null);
            vm.DefineFunctionN("@apply", parms => {
                var fun = parms.First();
                var args = Cons.Car(Cons.ConsFromArray(parms.Skip(1).ToArray()));
                return vm.Apply(fun, Cons.ToObjectArray(args));
            });
            vm.DefineFunction("eq?", ObjectEquals);
            vm.DefineFunction("@catch-error", CatchError);
            vm.DefineFunction("@run-template", RunTemplate);
            vm.DefineFunction("@load-lvm-file", LoadLVMFile);
            vm.DefineFunction("@print-profile-data", () => {
                throw new LunulaException("Profiler not enabled");
            });
            vm.DefineFunction("@make-type", (tag, data) => new TaggedType((Symbol)tag, data));
            vm.DefineFunction("@type-symbol", type => ((TaggedType)type).Tag);
            vm.DefineFunction("@type-data", type => ((TaggedType)type).Data);
            vm.DefineFunction("@type-data-set!", (type, data) => { ((TaggedType)type).Data = data; return Void.TheVoidValue; });
            vm.DefineFunction("call/cc", _vm.CallWithCurrentContinuation);
        }

        static object MakeVector(object list) {
            return Cons.ToObjectArray(list);
        }

        static object VectorRef(object v, object i) {
            return ((object[])v)[(int)(double)i];
        }

        static object VectorSet(object v, object i, object val) {
            ((object[])v)[(int)(double)i] = val;
            return Void.TheVoidValue;
        }

        static object VectorLength(object v) {
            return ((double)((object[])v).Length);
        }

        static object IsVector(object thing) {
            return (thing is object[]);
        }

        static object MakeHashTable() {
            return new Dictionary<object, object>();
        }

        static object HashSet(object table, object key, object value) {
            ((Dictionary<object, object>)table)[key] = value;
            return Void.TheVoidValue;
        }

        static object HashRef(object table, object key, object failureResult) {
            object value;
            return ((Dictionary<object, object>)table).TryGetValue(key, out value) ? value : failureResult;
        }

        static object HashRemove(object table, object key) {
            ((Dictionary<object, object>)table).Remove(key);
            return Void.TheVoidValue;
        }

        static object HashKeys(object table) {
            var d = (Dictionary<object, object>)table;
            return Cons.ConsFromIEnumerable(d.Keys);
        }

        static object HashValues(object table) {
            var d = (Dictionary<object, object>)table;
            return Cons.ConsFromIEnumerable(d.Values);
        }

        static object IsBoolean(object thing) {
            return thing is bool;
        }

        static object BooleanEquals(object a, object b) {
            return (bool)a == (bool)b;
        }

        static object IsChar(object thing) {
            return thing is char;
        }

        static object CharEquals(object a, object b) {
            return (char)a == (char)b;
        }

        static object IsCharAlphabetic(object ch) {
            return char.IsLetter((char)ch);
        }

        static object IsCharNumeric(object ch) {
            return char.IsDigit((char)ch);
        }

        static object CharCode(object ch) {
            return (double)(int)(char)ch;
        }

        static object IsSymbol(object thing) {
            return thing is Symbol;
        }

        static object SymbolEquals(object a, object b) {
            return a == b;
        }

        static object IsVoid(object thing) {
            return thing is Void;
        }

        static object MakeVoid() {
            return Void.TheVoidValue;
        }

        static object IsProcedure(object thing) {
            return
                thing is Closure ||
                thing is Func<object> ||
                thing is Func<object, object> ||
                thing is Func<object, object, object> ||
                thing is Func<object, object, object, object> ||
                thing is Func<object, object, object, object, object>;
        }

        object ToplevelIsDefined(object symbol) {
            return _vm.ToplevelIsDefined((Symbol)symbol);
        }

        object ToplevelLookup(object name) {
            return _vm.ToplevelLookup((Symbol)name);
        }

        object ToplevelDefine(object name, object value) {
            _vm.ToplevelDefine((Symbol)name, value);
            return Void.TheVoidValue;
        }

        static object IsCons(object thing) {
            return thing is Cons;
        }

        static object IsNull(object thing) {
            return thing == null;
        }

        static object IsNumber(object thing) {
            return thing is double;
        }

        static object NumberEqual(object a, object b) {
            return (double)a == (double)b;
        }

        static object NumberGreaterThan(object a, object b) {
            return (double)a > (double)b;
        }

        static object NumberGreaterThanAndEqual(object a, object b) {
            return (double)a >= (double)b;
        }

        static object NumberLessThan(object a, object b) {
            return (double)a < (double)b;
        }

        static object NumberLessThanAndEqual(object a, object b) {
            return (double)a <= (double)b;
        }

        static object TwoArgPlus(object a, object b) {
            return (double)a + (double)b;
        }

        static object TwoArgMinus(object a, object b) {
            return (double)a - (double)b;
        }

        static object TwoArgMultiply(object a, object b) {
            return (double)a * (double)b;
        }

        static object TwoArgDivide(object a, object b) {
            return (double)a / (double)b;
        }

        static object BinaryOr(object a, object b) {
            return (double)(((uint)(double)a) | ((uint)(double)b));
        }

        static object LeftShift(object a, object b) {
            return (double)(((uint)(double)a) << ((int)(double)b));
        }

        static Cons MakeCons(object a, object b) {
            return new Cons(a, b);
        }

        static object Car(object cons) {
            return Cons.Car(cons);
        }

        static object Cdr(object cons) {
            return Cons.Cdr(cons);
        }

        static object SetCar(object cons, object value) {
            return Cons.SetCar(cons, value);
        }

        static object SetCdr(object cons, object value) {
            return Cons.SetCdr(cons, value);
        }

        static object IsString(object thing) {
            return thing is string;
        }

        static object StringEquals(object a, object b) {
            return ((string)a).Equals((string)b);
        }

        static object StringLength(object str) {
            return (double)((string)str).Length;
        }

        static object StringAppend(object a, object b) {
            return (string)a + (string)b;
        }

        static object SymbolToString(object sym) {
            return ((Symbol)sym).Name;
        }

        static object NumberToString(object num) {
            return ((double)num).ToString();
        }

        static object OpenInputString(object str) { return new PeekableTextReaderAdapter(new StringReader((string)str)); }
        static object OpenOutputString() { return new StringWriter(); }
        static object GetOutputString(object port) { return port.ToString(); }

        static object OpenOutputByteArray() { return new BinaryWriter(new MemoryStream()); }
        static object GetOutputByteArray(object port) { return ((MemoryStream)((BinaryWriter)port).BaseStream).ToArray(); }

        static object WriteByte(object x, object port) {
            ((BinaryWriter)port).Write((byte)(double)x);
            return Void.TheVoidValue;
        }

        static object WriteWord(object x, object port) {
            ((BinaryWriter)port).Write((UInt16)(double)x);
            return Void.TheVoidValue;
        }

        static object WriteDWord(object x, object port) {
            ((BinaryWriter)port).Write((UInt32)(double)x);
            return Void.TheVoidValue;
        }

        static object GetStdOut() { return Console.Out; }
        static object GetStdIn() { return new PeekableTextReaderAdapter(Console.In); }
        static object GetStdError() { return Console.Error; }

        static object OpenFileInputPort(object filename) { return new PeekableTextReaderAdapter(new StreamReader((string)filename)); }
        static object CloseInputPort(object port) { ((PeekableTextReaderAdapter)port).Close(); return Void.TheVoidValue; }

        static object OpenFileOutputPort(object filename) { return new StreamWriter((string)filename); }
        static object OpenBinaryFileOutputPort(object filename) { return new BinaryWriter(new FileStream((string)filename, FileMode.Create)); }

        static object CloseOutputPort(object port) {
            if (port is StreamWriter) {
                ((StreamWriter)port).Close();
            } else {
                ((BinaryWriter)port).Close();
            }
            return Void.TheVoidValue;
        }

        static object WriteChar(object x, object port) { ((TextWriter)port).Write((char)x); return Void.TheVoidValue; }
        static object WriteString(object x, object port) { ((TextWriter)port).Write((string)x); return Void.TheVoidValue; }
        static object FlushOutput(object port) { ((TextWriter)port).Flush(); return Void.TheVoidValue; }

        static object GetTime() {
            return DateTime.Now;
        }

        static object TimeDifference(object a, object b) {
            return (((DateTime)a) - ((DateTime)b)).TotalMilliseconds;
        }

        static object StaticToString(object thing) {
            return thing.ToString();
        }

        static object IsEOFObject(object thing) { return thing is EOF; }

        static object ReadChar(object port) {
            var c = ((PeekableTextReaderAdapter)port).Read();
            return c == -1 ? (object)EOF.TheEOFValue : (char)c;
        }

        static object PeekChar(object port) {
            var c = ((PeekableTextReaderAdapter)port).Peek(0);
            return c == -1 ? (object)EOF.TheEOFValue : (char)c;
        }

        static object PeekCharSkip(object port, object n) {
            var c = ((PeekableTextReaderAdapter)port).Peek((int)(double)n);
            return c == -1 ? (object)EOF.TheEOFValue : (char)c;
        }

        static object StringToList(object str) {
            object cons = ((string)str).Aggregate<char, object>(null, (current, c) => new Cons(c, current));
            return Cons.Reverse(cons);
        }

        static object ListToString(object list) {
            string outStr = "";
            while (list != null) {
                outStr += (char)Cons.Car(list);
                list = Cons.Cdr(list);
            }
            return outStr;
        }

        static object StringToSymbol(object str) {
            return Symbol.Intern((string)str);
        }

        static object StringToNumber(object obj) {
            var str = (string)obj;
            if (!Char.IsDigit(str[0])) return false;
            try {
                return double.Parse(str);
            } catch (FormatException) {
                return false;
            }
        }

        static object Fail(object msg) {
            throw new LunulaException(msg.ToString());
        }

        object CatchError(object fun, object handlerFun) {
            try {
                return _vm.Apply(fun);
            } catch (Exception e) {
                return _vm.Apply(handlerFun, new Cons(e.Message, null));
            }
        }

        static object ObjectEquals(object a, object b) {
            if (a == null && b == null) return true;
            if (a == null) return false;
            if (b == null) return false;
            return a.Equals(b);
        }

        object RunTemplate(object byteArray) {
            var template = _vm.ReadLunulaCode(new BinaryReader(new MemoryStream((byte[])byteArray)));
            return _vm.RunTemplate(template);
        }

        object LoadLVMFile(object fileName) {
            return _vm.LoadLVMFile((string)fileName);
        }
    }

    public class Template {
        public object[] Literals;
        public Instruction[] Code;

        public Template(object[] literals, Instruction[] code) {
            Literals = literals;
            Code = code;
        }
    }

    public class Closure {
        public LexicalEnvironment Envt;
        public Template Template;

        public Closure(LexicalEnvironment envt, Template template) {
            Envt = envt;
            Template = template;
        }
    }

    public class Continuation {
        public Continuation CONT;
        public LexicalEnvironment ENVT;
        public object EVAL_STACK;
        public Template TEMPLATE;
        public uint PC;

        public Continuation(Continuation cont, LexicalEnvironment envt, object evalStack, Template template, uint pc) {
            CONT = cont;
            ENVT = envt;
            EVAL_STACK = evalStack;
            TEMPLATE = template;
            PC = pc;
        }
    }

    public class Instruction {
        public enum OpCodes {
            SaveContinuation = 0, // 0
            FetchLiteral,         // 1
            Push,                 // 2 
            Apply,                // 3
            Bind,                 // 4
            MakeClosure,          // 5
            ToplevelGet,          // 6
            ToplevelSet,          // 7
            LocalGet,             // 8
            LocalSet,             // 9
            Return,               // 10
            End,                  // 11
            Jump,                 // 12
            JumpIfFalse,          // 13
            BindVarArgs,          // 14
        };
        public OpCodes OpCode;
        public ushort A;
        public ushort B;
        public uint AX;

        public Instruction(uint code) {
            // 6 bits for the opcode
            // 13 bits for A
            // 13 bits for B
            OpCode = (OpCodes)(code & 0x3F);
            A = (ushort)((code >> 6) & 0x1FFF);
            B = (ushort)(code >> 19);
            AX = code >> 6;
        }
    }

    public class LunulaVM {
        readonly Dictionary<Symbol, object> _toplevelEnv = new Dictionary<Symbol, object>();

        public bool ToplevelIsDefined(Symbol name) {
            return _toplevelEnv.ContainsKey(name);
        }

        public object ToplevelLookup(Symbol name) {
            object value;
            if (!_toplevelEnv.TryGetValue(name, out value)) {
                throw new SymbolNotFoundException(name);
            }
            return value;
        }

        public object ToplevelLookup(string name) {
            return ToplevelLookup(Symbol.Intern(name));
        }

        public void ToplevelDefine(Symbol name, object value) {
            _toplevelEnv[name] = value;
        }

        public void ToplevelDefine(string name, object value) {
            ToplevelDefine(Symbol.Intern(name), value);
        }

        public void DefineFunctionN(string name, Func<object[], object> fun) {
            var symName = Symbol.Intern(name);
            ToplevelDefine(symName, fun);
        }

        public void DefineFunction(string name, Func<object> fun) {
            var symName = Symbol.Intern(name);
            ToplevelDefine(symName, fun);
        }

        public void DefineFunction(string name, Func<object, object> fun) {
            var symName = Symbol.Intern(name);
            ToplevelDefine(symName, fun);
        }

        public void DefineFunction(string name, Func<object, object, object> fun) {
            var symName = Symbol.Intern(name);
            ToplevelDefine(symName, fun);
        }

        public void DefineFunction(string name, Func<object, object, object, object> fun) {
            var symName = Symbol.Intern(name);
            ToplevelDefine(symName, fun);
        }

        public void DefineFunction(string name, Func<object, object, object, object, object> fun) {
            var symName = Symbol.Intern(name);
            ToplevelDefine(symName, fun);
        }

        public object Apply(object fun, params object[] argsList) {
            if (fun is Closure) {
                return RunClosure((Closure)fun, argsList);
            }
            if (fun is Func<object>) {
                return ((Func<object>)fun).Invoke();
            }
            if (fun is Func<object, object>) {
                return ((Func<object, object>)fun).Invoke(argsList[0]);
            }
            if (fun is Func<object, object, object>) {
                return ((Func<object, object, object>)fun).Invoke(argsList[0], argsList[1]);
            }
            if (fun is Func<object, object, object, object>) {
                return ((Func<object, object, object, object>)fun).Invoke(argsList[0], argsList[1], argsList[2]);
            }
            if (fun is Func<object, object, object, object, object>) {
                return ((Func<object, object, object, object, object>)fun).Invoke(argsList[0], argsList[1], argsList[2], argsList[3]);
            }
            if (fun is Func<object[], object>) {
                return ((Func<object[], object>)fun).Invoke(argsList);
            }
            throw new InvalidOperationException("Invalid function to apply");
        }

        // REGISTERS
        Continuation CONT;
        LexicalEnvironment ENVT;
        object EVAL_STACK;
        Template TEMPLATE;
        uint PC;
        object VALUE = Void.TheVoidValue;

        LunulaVM() {
            new Builtins(this);
        }

        public LunulaVM(Stream lvmStream)
            : this() {
            LoadLVMFromStream(lvmStream);
        }

        public LunulaVM(string bootstrapLvmFileName)
            : this() {
            using (var s = File.OpenRead(bootstrapLvmFileName)) {
                LoadLVMFromStream(s);
            }
        }

        bool _finished = false;
        void Return() {
            if (CONT != null) {
                ENVT = CONT.ENVT;
                PC = CONT.PC;
                TEMPLATE = CONT.TEMPLATE;
                EVAL_STACK = CONT.EVAL_STACK;
                CONT = CONT.CONT;
            } else {
                _finished = true;
            }
        }

        object RunClosure(Closure closure, params object[] args) {
            var PREV_CONT = CONT;
            var PREV_ENVT = ENVT;
            var PREV_EVAL_STACK = EVAL_STACK;
            var PREV_TEMPLATE = TEMPLATE;
            var PREV_PC = PC;

            CONT = null;
            ENVT = closure.Envt;
            EVAL_STACK = Cons.Reverse(Cons.ConsFromArray(args));
            TEMPLATE = closure.Template;
            PC = 0;
            VALUE = Void.TheVoidValue;

            try {
                while (true) {
                    Instruction i = TEMPLATE.Code[PC];
                    switch (i.OpCode) {
                        case Instruction.OpCodes.Push:
                            EVAL_STACK = new Cons(VALUE, EVAL_STACK);
                            PC++;
                            break;
                        case Instruction.OpCodes.Bind: {
                                var numOfBindings = (int)i.AX;
                                ENVT = new LexicalEnvironment(ENVT, numOfBindings);
                                for (var x = 0; x < numOfBindings; x++) {
                                    ENVT.Bindings[numOfBindings - 1 - x] = Cons.Car(EVAL_STACK);
                                    EVAL_STACK = Cons.Cdr(EVAL_STACK);
                                }
                                if (EVAL_STACK != null)
                                    throw new InvalidOperationException("Too many parameters given to function");
                                PC++;
                            }
                            break;
                        case Instruction.OpCodes.ToplevelGet:
                            VALUE = ToplevelLookup((Symbol)VALUE);
                            PC++;
                            break;
                        case Instruction.OpCodes.SaveContinuation:
                            CONT = new Continuation(CONT, ENVT, EVAL_STACK, TEMPLATE, i.AX);
                            EVAL_STACK = null;
                            PC++;
                            break;
                        case Instruction.OpCodes.FetchLiteral:
                            VALUE = TEMPLATE.Literals[i.AX];
                            PC++;
                            break;
                        case Instruction.OpCodes.LocalGet: {
                                LexicalEnvironment envt = ENVT;
                                for (int x = 0; x < i.A; x++)
                                    envt = envt.Parent;
                                VALUE = envt.Bindings[i.B];
                                PC++;
                            }
                            break;
                        case Instruction.OpCodes.JumpIfFalse: {
                                if (VALUE is bool && ((bool)VALUE == false))
                                    PC = i.AX;
                                else
                                    PC++;
                            }
                            break;
                        case Instruction.OpCodes.BindVarArgs: {
                                int numOfBindings = (int)i.AX;
                                ENVT = new LexicalEnvironment(ENVT, numOfBindings);

                                // parameters are reversed on EVAL stack
                                EVAL_STACK = Cons.Reverse(EVAL_STACK);

                                for (var x = 0; x < numOfBindings; x++) {
                                    // if it is the last binding, take the rest of the EVAL_STACK
                                    if (x == numOfBindings - 1) {
                                        ENVT.Bindings[x] = EVAL_STACK;
                                        EVAL_STACK = null;
                                    } else {
                                        ENVT.Bindings[x] = Cons.Car(EVAL_STACK);
                                        EVAL_STACK = Cons.Cdr(EVAL_STACK);
                                    }
                                }
                                PC++;
                            }
                            break;
                        case Instruction.OpCodes.MakeClosure:
                            VALUE = new Closure(ENVT, (Template)VALUE);
                            PC++;
                            break;
                        case Instruction.OpCodes.Return:
                            Return();
                            break;
                        case Instruction.OpCodes.ToplevelSet:
                            _toplevelEnv[(Symbol)VALUE] = Cons.Car(EVAL_STACK);
                            EVAL_STACK = Cons.Cdr(EVAL_STACK);
                            PC++;
                            break;
                        case Instruction.OpCodes.Apply: {
                                if (VALUE is Closure) {
                                    var clos = (Closure)VALUE;
                                    ENVT = clos.Envt;
                                    TEMPLATE = clos.Template;
                                    VALUE = Void.TheVoidValue;
                                    PC = 0;
                                } else if (VALUE is Func<object>) {
                                    var func = (Func<object>)VALUE;
                                    VALUE = func();
                                    Return();
                                } else if (VALUE is Func<object, object>) {
                                    var func = (Func<object, object>)VALUE;
                                    VALUE = func(Cons.Car(EVAL_STACK));
                                    Return();
                                } else if (VALUE is Func<object, object, object>) {
                                    var func = (Func<object, object, object>)VALUE;
                                    VALUE = func(Cons.Car(Cons.Cdr(EVAL_STACK)), Cons.Car(EVAL_STACK));
                                    Return();
                                } else if (VALUE is Func<object, object, object, object>) {
                                    var func = (Func<object, object, object, object>)VALUE;
                                    VALUE = func(Cons.Car(Cons.Cdr(Cons.Cdr(EVAL_STACK))), Cons.Car(Cons.Cdr(EVAL_STACK)), Cons.Car(EVAL_STACK));
                                    Return();
                                } else if (VALUE is Func<object, object, object, object, object>) {
                                    var func = (Func<object, object, object, object, object>)VALUE;
                                    VALUE = func(Cons.Car(Cons.Cdr(Cons.Cdr(Cons.Cdr(EVAL_STACK)))), Cons.Car(Cons.Cdr(Cons.Cdr(EVAL_STACK))), Cons.Car(Cons.Cdr(EVAL_STACK)), Cons.Car(EVAL_STACK));
                                    Return();
                                } else if (VALUE is Func<object[], object>) {
                                    var func = (Func<object[], object>)VALUE;
                                    VALUE = func(Cons.ToReverseObjectArray(EVAL_STACK));
                                    Return();
                                } else {
                                    throw new LunulaException("VALUE register does not contain a callable object: " + VALUE.ToString());
                                }
                            }
                            break;
                        case Instruction.OpCodes.LocalSet: {
                                LexicalEnvironment envt = ENVT;
                                for (int x = 0; x < i.A; x++)
                                    envt = envt.Parent;
                                envt.Bindings[i.B] = VALUE;
                                PC++;
                            }
                            break;
                        case Instruction.OpCodes.Jump:
                            PC = i.AX;
                            break;
                        case Instruction.OpCodes.End:
                            _finished = true;
                            break;
                        default:
                            throw new InvalidOperationException("Invalid instruction");
                    }
                    if (_finished) {
                        _finished = false;
                        return VALUE;
                    }
                }
            } finally {
                CONT = PREV_CONT;
                ENVT = PREV_ENVT;
                EVAL_STACK = PREV_EVAL_STACK;
                TEMPLATE = PREV_TEMPLATE;
                PC = PREV_PC;
            }
        }

        public object RunTemplate(Template template) {
            return RunClosure(new Closure(null, template));
        }

        static int ReadHeader(BinaryReader br) {
            // The header of a compile lunula file starts with
            // the chars LUNULA followed by a 16-bit version number
            if ((from c in "LUNULA" let ch = (char)br.ReadUInt16() where ch != c select c).Any()) {
                throw new InvalidOperationException("Invalid Lunula data stream");
            }
            return br.ReadUInt16();
        }

        static string ReadString(BinaryReader br) {
            int length = br.ReadUInt16();
            var chars = new char[length];
            for (int i = 0; i < length; i++) {
                chars[i] = (char)br.ReadInt16();
            }
            return new String(chars);
        }

        static Template ReadTemplate(BinaryReader br) {
            var version = br.ReadInt32();
            Debug.Assert(version == 3);
            var numberOfLiterals = br.ReadInt32();
            var numberOfInstructions = br.ReadInt32();
            var literals = new object[numberOfLiterals];
            var instructions = new Instruction[numberOfInstructions];
            for (int x = 0; x < numberOfLiterals; x++) {
                byte type = br.ReadByte();
                switch (type) {
                    // Symbol
                    case 1:
                        literals[x] = Symbol.Intern(ReadString(br));
                        break;
                    // String
                    case 2:
                        literals[x] = ReadString(br);
                        break;
                    // Number
                    case 3: {
                            var str = ReadString(br);
                            literals[x] = double.Parse(str);
                            break;
                        }
                    // Boolean
                    case 4:
                        literals[x] = br.ReadByte() == 0 ? false : true;
                        break;
                    // Null
                    case 5:
                        literals[x] = null;
                        break;
                    // Char
                    case 6:
                        literals[x] = (char)br.ReadUInt16();
                        break;
                    // Template
                    case 10:
                        literals[x] = ReadTemplate(br);
                        break;
                }
            }

            for (int x = 0; x < numberOfInstructions; x++) {
                instructions[x] = new Instruction(br.ReadUInt32());
            }

            var template = new Template(literals, instructions);
            return template;
        }

        public Template ReadLunulaCode(BinaryReader br) {
            int version = ReadHeader(br);
            if (version != 1) throw new InvalidOperationException("Invalid lunula version");
            return ReadTemplate(br);
        }

        public object LoadLVMFromStream(Stream s) {
            Template template;
            using (var br = new BinaryReader(s)) {
                template = ReadLunulaCode(br);
            }
            return RunTemplate(template);
        }

        public object LoadLVMFile(string fileName) {
            using (var s = File.OpenRead(fileName)) {
                return LoadLVMFromStream(s);
            }
        }

        public object Eval(string expression) {
            var readFromString = _toplevelEnv[Symbol.Intern("read-from-string")];
            var eval = _toplevelEnv[Symbol.Intern("eval")];
            return Apply(eval, Apply(readFromString, expression));
        }

        public object Load(string fileName) {
            return Apply(_toplevelEnv[Symbol.Intern("load")], fileName);
        }

        public object CallWithCurrentContinuation(object fun) {
            var cont = new Continuation(CONT, ENVT, EVAL_STACK, TEMPLATE, PC);
            object retval = Apply(fun, new Func<object, object>(r => {
                CONT = cont.CONT;
                ENVT = cont.ENVT;
                EVAL_STACK = cont.EVAL_STACK;
                TEMPLATE = cont.TEMPLATE;
                PC = cont.PC;
                retval = r;
                return r;
            }));
            return retval;
        }

    }
}