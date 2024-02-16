using System;
using TCLSHARP;

using MySql.Data.MySqlClient;


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

            interp.ns["mysql"] = new NetClass(typeof(MySqlConnection));
            /*
            var cc = new MySqlConnection();
            var cmd = cc.CreateCommand();

            cmd.CommandText = "SELECT * FROM men WHERE age = 22";
            var reader = cmd.ExecuteReader();
           
            while (reader.Read())
            {

                // элементы массива [] - это значения столбцов из запроса SELECT
                Console.WriteLine(reader[0].ToString() + " " + reader[1].ToString());
            }
            reader.Close(); // закрываем reader
            */
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
