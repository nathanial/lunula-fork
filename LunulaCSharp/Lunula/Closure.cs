namespace Lunula
{
    public class Closure {
        public LexicalEnvironment Envt;
        public Template Template;

        public Closure(LexicalEnvironment envt, Template template) {
            Envt = envt;
            Template = template;
        }
    }
}