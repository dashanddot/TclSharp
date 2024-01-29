using System;
using System.IO;

namespace TCLSHARP
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = "game.tcl";

            if (args.Length > 0)
                file = args[0];


            var text = File.ReadAllText(file);

            var app = TCL.parseTCL(text);

            var interp = new TCLInterp();

            foreach (var a in app)
            {
                interp.evalTclLine(a);
            }


        }
    }
}
//*/