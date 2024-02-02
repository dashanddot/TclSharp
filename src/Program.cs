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


        

            var interp = new TCLInterp(true);

            interp.Exec(file);

          


        }
    }
}
//*/