namespace Lunula
{
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
}