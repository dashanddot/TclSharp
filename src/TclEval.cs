using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace TCLSHARP
{

	class TCLProc
	{
		public TCLAtom[] args;
		public TCLAtom[] defs;

		public List<TCLAtom> code;

		public TCLAtom DoCall(TCLAtom[] argv)
		{
			var interp = TCLInterp.runningNow;

			var prew = interp.ns;

			interp.ns = new Dictionary<string, TCLAtom>(prew);

			for (int i = 0; i < this.args.Length; i++)
			{
				interp.ns[this.args[i]] = argv[i];
			}

			if (this.args.Length < argv.Length && this.args[this.args.Length-1] == "args" )
			{
				var args_segment = new TCLAtom[argv.Length - (this.args.Length-1)];

				Array.Copy(argv, this.args.Length-1, args_segment, 0, args_segment.Length);

				interp.ns["args"] = TCLAtom.auto( args_segment );
			}

			foreach (var a in code)
			{
				if (interp.returnValue != null)
					break;

				interp.evalTclLine(a);
			}

			var res = interp.returnValue;
			interp.returnValue = null;

			return res;
		}
	}

	public class TCLInterp : ITCLInterp
	{
		public string[] searchPathes;

		public delegate int PerformCalculation(TCLAtom[] argv);

		public TCLAtom conio_gets(TCLAtom[] argv)
		{
			var interp = TCLInterp.runningNow;

			

			interp.ns[ argv[1] ] = Console.ReadLine();

			return null;
		}

		public TCLAtom conio_flush(TCLAtom[] argv)
		{

			return null;
		}

		public TCLAtom source_tcl(TCLAtom[] argv)
		{
			return _execSource(TCLInterp.runningNow,argv[0]);
		}

		TCLAtom _execSource(TCLInterp interp, string file)
		{
			var text = System.IO.File.ReadAllText(file);

			var app = TCL.parseTCL(text);

			

			foreach (var a in app)
			{
				if (interp.returnValue != null)
					break;

				interp.evalTclLine(a);
			}

			return interp.returnValue;
		}

		public TCLAtom debug_print(TCLAtom[] argv)
		{
			bool newline = true;

			foreach (var s in argv)
			{
				var ss = s.ToString();

				if (ss[0] == '-')
				{
					if (ss == "-nonewline")
						newline = false;

					continue;
				}

				Console.Write(s.ToString());
			}

			if(newline)
			Console.WriteLine();

			return null;
		}
		public TCLAtom proc_define(TCLAtom[] argv)
		{


			

			return creteProcedure(argv[0], TCL.parseTCL(argv[1]), TCL.parseTCL(argv[2]));
		}
		public TCLAtom creteProcedure( string name, List<TCLAtom> args, List<TCLAtom> code )
		{
			var proc = new TCLProc();

			

			if ( args != null && args.Count > 0)
				proc.args = args[0].Slice(0);
			else
				proc.args = new TCLAtom[0];

			proc.code = code;

			var procObj = TCLAtom.func(proc.DoCall);

			if (name != null)
				ns[name] = procObj;

			return procObj;
		}

		TCLAtom _nproc(TCLAtom[] argv)
		{
			return TCLObject.auto(System.Environment.TickCount);
		}

		public TCLAtom namespace_define(TCLAtom[] argv)
		{
			return null;
		}

		public TCLAtom cmd_if_define(TCLAtom[] argv)
		{
			var interp = TCLInterp.runningNow;

			

			

			

			for (int i = 0; i < argv.Length; i++)
			{
				var expr = new TclExpr();

				var flag = TCLAtom.bool_true;

				if (argv[i] != "else")
				{
					i++;

					flag = expr.evalTCLexpr(argv[i], interp);
				}

				i++;

				

				if ( (bool)flag )
				{
					var code = TCL.parseTCL(argv[i]);

					foreach (var a in code)
					{
						if (interp.returnValue != null)
							break;

						interp.evalTclLine(a);
					}

					break;

				}
			}

			return null;
		}

		private TCLAtom cmd_incr(TCLAtom[] argv)
		{
			var interp = TCLInterp.runningNow;

			interp.ns[argv[1]]  = TCLObject.auto( (int)interp.ns[argv[1]] + 1);

			return interp.ns[argv[1]];
		}

		public TCLAtom while_define(TCLAtom[] argv)
		{
			var interp = TCLInterp.runningNow; 
			
			var expr = new TclExpr();

			

			var code = TCL.parseTCL(argv[1]);

			int loop = 0;

			while (true)
			{
				var tcllines = TCL.parseTCL(argv[0]);

				var finaltext = cmd_argv(TCLAtom.auto(tcllines[0]), 0);

				var flag = expr.evalTCLexpr(finaltext[0], interp);

				if (!(bool)flag)
					break;

				foreach (var a in code)
				{
					if (interp.returnValue != null)
						break;

					interp.evalTclLine(a);
				}

				if(++loop>300)
					break;
			}

			return null;
		}

		public TCLAtom set_var(TCLAtom[] argv)
		{
			ns[argv[0]] = argv[1];

			return argv[1];
		}

		public TCLAtom expr_do(TCLAtom[] argv)
		{
			string exprs = "";

			foreach (var a in argv)
			{
				exprs += a.ToString();
			}

			return (new TclExpr()).evalTCLexpr(exprs, this);
		}

		public TCLAtom clock_get(TCLAtom[] argv)
		{
			return TCLObject.auto(System.Environment.TickCount/1000);
		}

		public TCLAtom math_rand(TCLAtom[] argv)
		{
			return TCLObject.auto( (new Random()).NextDouble() );
		}

		public TCLAtom math_round(TCLAtom[] argv)
		{
			return TCLObject.auto( (int)Math.Round((double)argv[0])  );
		}
		public TCLAtom break_return(TCLAtom[] argv)
		{
			if (argv.Length > 0)
				TCLInterp.runningNow.returnValue = argv[0];
			else
				TCLInterp.runningNow.returnValue = TCLObject.nil;

			return TCLInterp.runningNow.returnValue;
		}

		public Dictionary<string, TCLAtom> ns = new Dictionary<string, TCLAtom>();

		public TCLInterp( bool loadRuntime = false)
		{
			ns.Add("tclr", new NetClass( typeof(TclNetRuntime) ) );

			//ns.Add("::tclr::extern", TCLObject.def_cmd(clr_extern));

			ns.Add("source", TCLAtom.func(source_tcl)); 
			ns.Add("namespace", TCLAtom.def_cmd(namespace_define));
			ns.Add("proc", TCLAtom.func(proc_define));
			ns.Add("set", TCLAtom.func(set_var));

			ns.Add("while", TCLAtom.func(while_define));
			ns.Add("if", TCLAtom.def_cmd(cmd_if_define));

			ns.Add("expr", TCLAtom.func(expr_do));
			ns.Add("return", TCLAtom.func(break_return));

			ns.Add("package", TCLAtom.func(Package.cmd_package));

			//TODO: move to tclr
			ns.Add("puts", TCLAtom.func(debug_print));

			//todo: move to implementation?
			ns.Add("gets", TCLAtom.func( conio_gets ));
			ns.Add("flush", TCLAtom.func(conio_flush));

			//TODO: to stdlib
			ns.Add("clock", TCLAtom.func(clock_get));

			//TODO: math?
			ns.Add("rand", TCLAtom.func(math_rand));
			ns.Add("round", TCLAtom.func(math_round));
			ns.Add("incr", TCLAtom.def_cmd(  cmd_incr ));


			var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

			if (loadRuntime)
				_execSource(this, path + Path.DirectorySeparatorChar + "tclr.tcl");
		}



		string stringTcl(TCLAtom cmd)
		{
			var list = evalTclOrString(cmd);

			string sout = "";

			if (list.oo is string)
				return list.oo as string;

			if (list.is_array && list.oo is TCLAtom[] )
				return list.ToString();

			foreach (var s in list.oo as List<TCLAtom>)
				sout += s;

			return sout;
		}


		TCLAtom evalTclOrString(TCLAtom cmd)
		{



			if ((cmd).is_array)
			{
				return evalTclLine(cmd);
			}



			string ss = cmd;
			int ssl = (ss).Length;

			//echo "stringTcl '".print_r( cmd, true )."' ssl;";

			int sua = 0;
			int sul = 0;

			List<TCLAtom> _list = new List<TCLAtom>();

			for (int i = 0; i < ssl; i++)
			{
				if (sua == -1)
					sua = i;

				if (ss[i] == '/')
				{
					if (sul > 0)
						_list.Add(ss.Substring(sua, sul));

					i++;
					_list.Add(ss.Substring(i, 1));
					

					sua = -1;
					sul = 0;//string length is 1 (+breakked of var)

					continue;
				}
					// echo "tok ".ss[i];
				if (ss[i] == '$')
				{
					if(sul > 0)
						_list.Add(ss.Substring(sua, sul));


					sul = 0;

					var arr = new ReadVarResult();

					TCL.readVariable(ss, i, arr);



					if (arr.val != null)
					{
						TCLAtom kvar = ns[arr.key];
						var key = stringTcl(arr.val);

						//echo "kv 'var' 'key' ";

						_list.Add( kvar[key] );
					}
					else
						_list.Add( ns[arr.key] );

					i = arr.pos-1;//readVariable return pos of break
					sua = -1;
					sul = 0;//string length is 1 (+breakked of var)

				}
				else if (ss[i] == '[')
				{
					if (sul > 0)
						_list.Add(ss.Substring(sua, sul));

					var arr = new ReadVarResult();

					TCL.readSqure(ss, i, arr);

					_list.Add(evalTclScript(TCL.parseTCL(arr.key, false)) );//TODO:recursive

					i = arr.pos;//skip ]
					sua = -1;
					sul = 0;//string length is 1 (+breakked of var)

					
				}
				else
				{
					sul++;
				}

			}

			if (sul > 0 && ss.Length > sua )
				_list.Add( ss.Substring(sua, sul) );

			if( _list.Count == 1 )
				return _list[0];

			return TCLObject.auto( _list );
		}

		public static TCLInterp runningNow = null;
		public TCLAtom returnValue;

		protected TCLAtom _unknown = null;

		public TCLAtom evalTclLine(TCLAtom cmd)
		{
			runningNow = this;

			if ((cmd).is_array)
			{
				var cmdname = cmd[0].ToString();

				if (cmdname[0] == '#')
					return null;

				if (cmdname.Contains("$") || cmdname[0] == '[')
				{
					var xargv = cmd_argv(cmd,0,1);

					if (xargv[0] is TCLObject)
					{
						return xargv[0].Command(this, cmd);
					}

					cmdname = xargv[0];

				}

				if (cmdname.Contains("::"))
				{
					

					return resolveQName(cmdname).Command(this,cmd);

				}

				if (ns.ContainsKey(cmdname))
				{
					//echo "call (func) ";
					var func = ns[cmdname];

					

					//echo "args:  ".print_r( argv, true );

					//argv = array_map('TCL::stringTcl', argv);

					return func.Command( this, cmd );
				}
				else
				{
					if (_unknown == null)
						_unknown = ns.ContainsKey( "unknown" ) ?  ns[ "unknown" ] : null;

					if (_unknown != null)
					{
						return _unknown.Command(this, cmd);
					}

					return erro_print_r(cmdname, true);
				}
			}

			return cmd;
		}

		private TCLAtom resolveQName(string cmdname)
		{
			if (ns.ContainsKey(cmdname))
				return ns[cmdname];

			var qname = cmdname.Split("::");

			TCLAtom target = null;
			int pos = 0;

			if (qname[0] == "")
			{
				target = ns[qname[1]];//FIXME: root qname here
				pos+=2;
			}

			while (pos < qname.Length)
			{
				target = target[qname[pos++]];
			}

			return target;
		}

		public TCLAtom commandCall( string cmdname, TCLAtom[] argv )
		{
			var func = ns[cmdname];

			return func.Call(argv);
		}

		public TCLAtom[] cmd_argv(TCLAtom cmd, int v, int maxnn = -1 )
		{
			int nn = cmd.Count - v;
			var _out = new TCLAtom[nn];

			if (maxnn != -1)
				nn = Math.Min(nn, maxnn);

			for (int i = 0; i < nn; i++)
			{
				int from = i + v;
				if (cmd[from].kind == TCLKind.evstring)
					_out[i] = stringTcl(cmd[from]);
				else if (cmd[from].oo is string)
				{
					var arg = cmd[from].oo as string;

					if (string.IsNullOrEmpty(arg))
					{
						_out[i] = "";
						continue;
					}

					if (arg[0] == '$')
						_out[i] = ns[arg.Substring(1)];
					else if (arg[0] == '[')
						_out[i] = evalTclOrString(cmd[from]);
					else
						_out[i] = cmd[from];
				}
				else
					_out[i] = cmd[from];
			}

			return _out;
		}

		private TCLAtom evalTclScript(List<TCLAtom> list)
		{
			TCLAtom res = null;

			foreach (var a in list)
			{
				res = evalTclLine(a);
			}

			return res;
		}

		private TCLAtom erro_print_r(TCLAtom func, bool v)
		{
			Console.WriteLine("error:" + func.ToString());

			return null;
		}

		public object Exec(string file)
		{
			var text = File.ReadAllText(file);

			var app = TCL.parseTCL(text);

			foreach (var a in app)
			{
				if (returnValue != null)
					break;

				evalTclLine(a);
			}

			return returnValue;
		}
	}
}
