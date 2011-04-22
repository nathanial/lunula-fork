namespace Lunula
{
    public class Template {
        public object[] Literals;
        public Instruction[] Code;

        public Template(object[] literals, Instruction[] code) {
            Literals = literals;
            Code = code;
        }
    }
}