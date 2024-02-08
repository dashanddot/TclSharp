using System;
using TCLSHARP;

namespace TCLSH
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = null;

            if (args.Length > 0)
                file = args[0];

            var interp = new TCLInterp(true);

            if (!string.IsNullOrEmpty(file))
            {
                interp.Exec(file);
                return;
            }

            while (true)
            {
                Console.Write(">>");

                var line = Console.ReadLine();

                var tclline = TCL.parseTCL( line );

                interp.evalTclLine(tclline[0]);

                if (interp.returnValue != null)
                    break;
            }

            Console.WriteLine(interp.returnValue!=null ? interp.returnValue.ToString() : "0" );

        }
    }
}
