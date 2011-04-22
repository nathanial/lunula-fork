namespace Lunula
{
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
}