using System.Reflection;

namespace Lunula {
    public class Program {
        static void Main(string[] args) {
            LunulaVM vm;
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Lunula.lunula.lvm")) {
                vm = new LunulaVM(s);
            }

            if (args.Length > 0) {
                foreach (var arg in args) {
                    vm.Load(arg);
                }
            } else {
                vm.Eval("(repl)");
            }
        }
    }
}
