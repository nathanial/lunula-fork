using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using Lunula;
using System.IO;
using System.Diagnostics;

namespace LunulaCSharp {
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
