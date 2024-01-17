using System;
using System.IO;

namespace TCLSHARP
{
    class Program
    {
        static void Main(string[] args)
        {

            
            var text = File.ReadAllText("game.tcl");

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