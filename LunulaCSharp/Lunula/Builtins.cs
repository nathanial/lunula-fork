using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lunula
{
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
            vm.DefineFunction("@exit", x => Void.TheVoidValue);
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
            if (str.Any(x => !Char.IsDigit(x) && x != '.' && x != '-')) return false;
            if (str.Length == 1 && str == "-") return false;
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
}