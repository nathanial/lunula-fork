using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Lunula {
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
        Continuation _cont;
        LexicalEnvironment _envt;
        object _evalStack;
        Template _template;
        uint _pc;
        object _value = Void.TheVoidValue;

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

        bool _finished;
        void Return() {
            if (_cont != null) {
                _envt = _cont.ENVT;
                _pc = _cont.PC;
                _template = _cont.TEMPLATE;
                _evalStack = _cont.EVAL_STACK;
                _cont = _cont.CONT;
            } else {
                _finished = true;
            }
        }

        object RunClosure(Closure closure, params object[] args) {
            var prevCONT = _cont;
            var prevENVT = _envt;
            var prevEvalStack = _evalStack;
            var prevTEMPLATE = _template;
            var prevPC = _pc;

            _cont = null;
            _envt = closure.Envt;
            _evalStack = Cons.Reverse(Cons.ConsFromArray(args));
            _template = closure.Template;
            _pc = 0;
            _value = Void.TheVoidValue;

            try {
                while (true) {
                    var i = _template.Code[_pc];
                    switch (i.OpCode) {
                        case Instruction.OpCodes.Push:
                            _evalStack = new Cons(_value, _evalStack);
                            _pc++;
                            break;
                        case Instruction.OpCodes.Bind: {
                                var numOfBindings = (int)i.AX;
                                _envt = new LexicalEnvironment(_envt, numOfBindings);
                                for (var x = 0; x < numOfBindings; x++) {
                                    _envt.Bindings[numOfBindings - 1 - x] = Cons.Car(_evalStack);
                                    _evalStack = Cons.Cdr(_evalStack);
                                }
                                if (_evalStack != null)
                                    throw new InvalidOperationException("Too many parameters given to function");
                                _pc++;
                            }
                            break;
                        case Instruction.OpCodes.ToplevelGet:
                            _value = ToplevelLookup(_value as Symbol);
                            _pc++;
                            break;
                        case Instruction.OpCodes.SaveContinuation:
                            _cont = new Continuation(_cont, _envt, _evalStack, _template, i.AX);
                            _evalStack = null;
                            _pc++;
                            break;
                        case Instruction.OpCodes.FetchLiteral:
                            _value = _template.Literals[i.AX];
                            _pc++;
                            break;
                        case Instruction.OpCodes.LocalGet: {
                                LexicalEnvironment envt = _envt;
                                for (int x = 0; x < i.A; x++)
                                    envt = envt.Parent;
                                _value = envt.Bindings[i.B];
                                _pc++;
                            }
                            break;
                        case Instruction.OpCodes.JumpIfFalse: {
                                if (_value is bool && ((bool)_value == false))
                                    _pc = i.AX;
                                else
                                    _pc++;
                            }
                            break;
                        case Instruction.OpCodes.BindVarArgs: {
                                var numOfBindings = (int)i.AX;
                                _envt = new LexicalEnvironment(_envt, numOfBindings);

                                // parameters are reversed on EVAL stack
                                _evalStack = Cons.Reverse(_evalStack);

                                for (var x = 0; x < numOfBindings; x++) {
                                    // if it is the last binding, take the rest of the EVAL_STACK
                                    if (x == numOfBindings - 1) {
                                        _envt.Bindings[x] = _evalStack;
                                        _evalStack = null;
                                    } else {
                                        _envt.Bindings[x] = Cons.Car(_evalStack);
                                        _evalStack = Cons.Cdr(_evalStack);
                                    }
                                }
                                _pc++;
                            }
                            break;
                        case Instruction.OpCodes.MakeClosure:
                            _value = new Closure(_envt, _value as Template);
                            _pc++;
                            break;
                        case Instruction.OpCodes.Return:
                            Return();
                            break;
                        case Instruction.OpCodes.ToplevelSet:
                            _toplevelEnv[_value as Symbol] = Cons.Car(_evalStack);
                            _evalStack = Cons.Cdr(_evalStack);
                            _pc++;
                            break;
                        case Instruction.OpCodes.Apply: {
                                if (_value is Closure) {
                                    var clos = _value as Closure;
                                    _envt = clos.Envt;
                                    _template = clos.Template;
                                    _value = Void.TheVoidValue;
                                    _pc = 0;
                                } else if (_value is Func<object>) {
                                    var func = _value as Func<object>;
                                    _value = func();
                                    Return();
                                } else if (_value is Func<object, object>) {
                                    var func = _value as Func<object, object>;
                                    _value = func(Cons.Car(_evalStack));
                                    Return();
                                } else if (_value is Func<object, object, object>) {
                                    var func = _value as Func<object, object, object>;
                                    _value = func(Cons.Car(Cons.Cdr(_evalStack)), Cons.Car(_evalStack));
                                    Return();
                                } else if (_value is Func<object, object, object, object>) {
                                    var func = _value as Func<object, object, object, object>;
                                    _value = func(Cons.Car(Cons.Cdr(Cons.Cdr(_evalStack))), Cons.Car(Cons.Cdr(_evalStack)), Cons.Car(_evalStack));
                                    Return();
                                } else if (_value is Func<object, object, object, object, object>) {
                                    var func = _value as Func<object, object, object, object, object>;
                                    _value = func(Cons.Car(Cons.Cdr(Cons.Cdr(Cons.Cdr(_evalStack)))), Cons.Car(Cons.Cdr(Cons.Cdr(_evalStack))), Cons.Car(Cons.Cdr(_evalStack)), Cons.Car(_evalStack));
                                    Return();
                                } else if (_value is Func<object[], object>) {
                                    var func = _value as Func<object[], object>;
                                    _value = func(Cons.ToReverseObjectArray(_evalStack));
                                    Return();
                                } else {
                                    throw new LunulaException("VALUE register does not contain a callable object: " + _value);
                                }
                            }
                            break;
                        case Instruction.OpCodes.LocalSet: {
                                var envt = _envt;
                                for (var x = 0; x < i.A; x++)
                                    envt = envt.Parent;
                                envt.Bindings[i.B] = _value;
                                _pc++;
                            }
                            break;
                        case Instruction.OpCodes.Jump:
                            _pc = i.AX;
                            break;
                        case Instruction.OpCodes.End:
                            _finished = true;
                            break;
                        default:
                            throw new InvalidOperationException("Invalid instruction");
                    }
                    if (_finished) {
                        _finished = false;
                        return _value;
                    }
                }
            } finally {
                _cont = prevCONT;
                _envt = prevENVT;
                _evalStack = prevEvalStack;
                _template = prevTEMPLATE;
                _pc = prevPC;
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

            for (var x = 0; x < numberOfInstructions; x++) {
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
            var cont = new Continuation(_cont, _envt, _evalStack, _template, _pc);
            object retval = Apply(fun, new Func<object, object>(r => {
                _cont = cont.CONT;
                _envt = cont.ENVT;
                _evalStack = cont.EVAL_STACK;
                _template = cont.TEMPLATE;
                _pc = cont.PC;
                retval = r;
                return r;
            }));
            return retval;
        }

    }
}