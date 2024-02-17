using System;
using System.IO;
using TCLSHARP;

using System.Collections.Generic;

namespace TCLCC
{




    class XTL
    {
        public static Dictionary<string, TCLAtom> defines = new Dictionary<string, TCLAtom>();

        static TCLAtom[] cmd_argv(TCLAtom cmd, int v)
        {
            var res = new TCLAtom[cmd.Count - v];

            for (int i = 0; i < res.Length; i++)
            {
                res[i] = cmd[i + v];
            }

            return res;
        }

        public static TCLAtom define(TCLAtom[] argv)
        {
            var defArgs = cmd_argv(argv, 1);






            var proc = TCLInterp.runningNow.creteProcedure("define_" + defArgs[0], TCL.parseTCL(defArgs[1]), TCL.parseTCL(defArgs[2]));


            defines[argv[1].ToString()] = proc;

            return proc;
        }

        internal static TCLInterp interp;
        internal static TCLInterp xtl;
        internal static Dictionary<string, TCLAtom> ns;

        public static TCLAtom unknown(TCLAtom[] argv)
        {
            
            {
               
                _writetext(argv[0] != null ? argv[0].ToString() : "");
                _writetext( "( " );

                for (int i = 1; i < argv.Length; i++)
                {
                    if (i > 1)
                        _writetext(", ");

                    if (argv[i].kind == TCLKind.evstring )
                        _writetext( "\"" + argv[i].ToString() + "\"" );
                    else
                        _writetext(  argv[i].ToString() );

                   
                }

                _writetext(" )");
            }
           


            return TCLAtom.nil;
        }

        internal static void ExecFile(string file)
        {
            var text = File.ReadAllText(file);

            ExecText(text);
        }

        internal static void ExecText(string text, bool endline = true)
        { 
            var app = TCL.parseTCL(text);

            foreach (var a in app)
            {
                for (int i = 1; i <= _block; i++) 
                    _writetext( " ");

                evalTclLine(a, endline);
            }

           
        }

        private static TCLAtom evalTclLine(TCLAtom cmd, bool endline = true)
        {
            bool stmnt = _evalTclLine(cmd);

            if (endline && stmnt)
                _writetext(";\n");

            return null;
        }
        private static bool _evalTclLine(TCLAtom cmd)
        {
            if ((cmd).is_array)
            {
                var cmdname = cmd[0].ToString();

                if (cmdname[0] == '#'  )
                {
                    ns["comment"].Call(cmd_argv(cmd,0) );

                    return false;
                }

                if (cmdname.Contains("::"))
                {


                    resolveQName(cmdname).Call( cmd);

                    return false;
                }

                if (ns.ContainsKey(cmdname))
                {
                    //echo "call (func) ";
                    var func = ns[cmdname];



                    //echo "args:  ".print_r( argv, true );

                    //argv = array_map('TCL::stringTcl', argv);

                    return func.Call( cmd_argv(cmd,1) );

                     
                }
                else
                {
                    unknown( cmd_argv(cmd, 0) );
                    

                    return true;
                }
            }



            return true;
        }

        private static TCLAtom resolveQName(string cmdname)
        {
            if (ns.ContainsKey(cmdname))
                return ns[cmdname];

            var qname = cmdname.Split("::");

            TCLAtom target = null;
            int pos = 0;

            if (qname[0] == "")
            {
                target = ns[qname[1]];//FIXME: root qname here
                pos += 2;
            }

            while (pos < qname.Length)
            {
                target = target[qname[pos++]];
            }

            return target;
        }

        static int _block = 0;
        internal static TCLAtom codeblock(TCLAtom[] arg)
        {
            _block++;

            starttext(null);
            ExecText(arg[1]);
            var _end = endtext(null);

            _block--;


            _writetext(_end);

            return null;
        }

        internal static TCLAtom codeline(TCLAtom[] arg)
        {
            var argString = arg[1].ToString();

            starttext(null);

            if ( argString[0] == '[')
                ExecText(argString.Substring(1,argString.Length-2),false);

            var text = endtext(null);

            return TCLAtom.auto(text );
        }

        static string _textbuf = null;
        static Stack<string> _textstack = new Stack<string>();

        internal static TCLAtom starttext(TCLAtom[] arg)
        {
            if (_textbuf != null)
                _textstack.Push(_textbuf);

            _textbuf = "";

            return null;
        }

        internal static TCLAtom endtext(TCLAtom[] arg)
        {
            var prew = _textbuf;

            _textbuf = null;

            if (_textstack.Count > 0)
                _textbuf = _textstack.Pop();

            return prew;
        }

        internal static TCLAtom depth(TCLAtom[] arg)
        {
            return TCLAtom.auto(_textstack.Count);
        }

        static bool _newline = false;

        internal static TCLAtom _writetext( string text )
        {
            

           

            if (_textbuf != null)
                _textbuf += text;
            else 
                Console.Write (text );

            return null;
        }

        internal static TCLAtom writeline(TCLAtom[] argv)
        {
            write(argv);
            _writetext("\n");

           

            return null;
        }

        internal static TCLAtom write(TCLAtom[] argv)
        {
            for (int i = 1; i < argv.Length; i++)
            {
                if (argv[i].kind == TCLKind.evstring)
                    _writetext("\"" + argv[i].ToString() + "\"");
                else
                    _writetext(argv[i].ToString());

            }

           

            return null;
        }

        internal static TCLAtom expline(TCLAtom[] argv)
        {
            string exprs = "";

            Dictionary<string, string> _temp = new Dictionary<string, string>();

            int from = 1;

            if (argv[1] == "-apply")
            {
                if (argv[2].is_array)
                {
                    argv = argv[2];
                    from = 0;

                }
                else
                {
                    from = 2;
                }


           }

           for( int i=from; i<argv.Length; i++)
            {
                var op = argv[i].ToString();

                if (op[0] == '[')
                {
                    starttext(null);
                    ExecText(op.Substring(1,op.Length-2), false );

                    op = endtext(null);

                    var varname = "$%temp" + i + "%";
                    _temp[varname] = op;

                    op =  varname;
                }

                exprs += op;
            }

            foreach ( var vv in _temp )
            {
                exprs = exprs.Replace(vv.Key, vv.Value);
            }

           

            return TCLAtom.auto( exprs );
        }


    }

    class TCLCC
    {
        static void Main(string[] args)
        {
            string file = "";
            string backend = "";

            int script_args = -1;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.Length> 1 && arg[0] == '-')
                {
                    if (arg[1] == '-')
                    {
                        script_args = i + 1;
                        break;
                    }

                    switch (arg.Substring(1))
                    {
                        case "backend":
                            {
                                backend = args[i + 1];
                                break;
                            }
                    }

                    continue;
                }

               
                file = args[i];

            }

            if (string.IsNullOrEmpty(file))
            {
                Console.WriteLine( "usage: ?-backend lua? file.tcl ?-- interpret args? " );
                return;
            }

            var interp = new TCLInterp(true);

            XTL.defines["comment"] = TCLObject.def_cmd((e) => TCLAtom.nil);
            XTL.defines["unknown"] = TCLObject.def_cmd(XTL.unknown);

            if (!string.IsNullOrEmpty(backend))
            {
                interp.ns["define"] = TCLObject.def_cmd( XTL.define );
                interp.Exec( $"backend/{backend}/rules.tcl" );
            }

            interp.ns["xtl::write"] = TCLObject.def_cmd(XTL.write);
            interp.ns["xtl::writeline"] = TCLObject.def_cmd(XTL.writeline);
            interp.ns["xtl::depth"] = TCLObject.def_cmd(XTL.depth);

            interp.ns["xtl::args"] = TCLObject.def_cmd( (aa) => TCLObject.auto( "<args>" ) );

            interp.ns["xtl::codeblock"] = TCLObject.def_cmd( XTL.codeblock );
            interp.ns["xtl::codeline"] = TCLObject.def_cmd(XTL.codeline);

            interp.ns["xtl::expr"] = TCLObject.def_cmd(XTL.expline);



            XTL.ns = new Dictionary<string, TCLAtom>( XTL.defines );

            XTL.interp = interp;

            
           

            XTL.ExecFile(file);




        }
    }
}
//*/