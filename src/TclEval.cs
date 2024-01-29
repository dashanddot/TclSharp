using System;
using System.Collections.Generic;
using System.Text;

namespace TCLSHARP
{
	class TCLProc
	{
		public TCLObject[] args;
		public TCLObject[] defs;

		public List<TCLObject> code;

		public TCLObject DoCall(TCLObject[] argv)
		{
			var interp = TCLInterp.runningNow;

			var prew = interp.ns;

			interp.ns = new Dictionary<string, TCLObject>(prew);

			for (int i = 0; i < this.args.Length; i++)
			{
				interp.ns[this.args[i]] = argv[i];
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
		public delegate int PerformCalculation(TCLObject[] argv);

		public TCLObject conio_gets(TCLObject[] argv)
		{
			var interp = TCLInterp.runningNow;

			

			interp.ns[ argv[1] ] = Console.ReadLine();

			return null;
		}

		public TCLObject conio_flush(TCLObject[] argv)
		{

			return null;
		}

		public TCLObject debug_print(TCLObject[] argv)
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
		public TCLObject proc_define(TCLObject[] argv)
		{
			var proc = new TCLProc();

			proc.args = TCL.parseTCL(argv[1])[0].Slice(0);
			proc.code = TCL.parseTCL(argv[2]);

			ns[argv[0]] = TCLObject.func(proc.DoCall);

			return ns[argv[0]];
		}

		public TCLObject if_define(TCLObject[] argv)
		{
			var interp = TCLInterp.runningNow;

			var expr = new TclExpr();

			expr.evalTCLexpr(argv[0], interp);

			var code = TCL.parseTCL(argv[1]);



			if (true)
			{
				foreach (var a in code)
				{
					if (interp.returnValue != null)
						break;

					interp.evalTclLine(a);
				}


				
			}

			return null;
		}

		public TCLObject while_define(TCLObject[] argv)
		{
			var interp = TCLInterp.runningNow; 
			
			var expr = new TclExpr();

			expr.evalTCLexpr(argv[0], interp);

			var code = TCL.parseTCL(argv[1]);

			

			while (true)
			{
				foreach (var a in code)
				{
					if (interp.returnValue != null)
						break;

					interp.evalTclLine(a);
				}


				break;
			}

			return null;
		}

		public TCLObject set_var(TCLObject[] argv)
		{
			ns[argv[0]] = argv[1];

			return argv[1];
		}

		public TCLObject expr_do(TCLObject[] argv)
		{
			string exprs = "";

			foreach (var a in argv)
			{
				exprs += a.ToString();
			}

			return (new TclExpr()).evalTCLexpr(exprs, this);
		}

		public TCLObject clock_get(TCLObject[] argv)
		{
			return TCLObject.auto(System.Environment.TickCount/1000);
		}

		public TCLObject math_rand(TCLObject[] argv)
		{
			return TCLObject.auto( (new Random()).NextDouble() );
		}

		public TCLObject math_round(TCLObject[] argv)
		{
			return TCLObject.auto( 0 );
		}
		public TCLObject break_return(TCLObject[] argv)
		{
			TCLInterp.runningNow.returnValue = argv[0];

			return TCLInterp.runningNow.returnValue;
		}

		public Dictionary<string, TCLObject> ns = new Dictionary<string, TCLObject>();

		public TCLInterp()
		{
			ns.Add("puts", TCLObject.func(debug_print));

			//todo: move to implementation?
			ns.Add("gets", TCLObject.func( conio_gets ));
			ns.Add("flush", TCLObject.func(conio_flush));


			ns.Add("proc", TCLObject.func(proc_define));
			ns.Add("set", TCLObject.func(set_var));

			ns.Add("while", TCLObject.func(while_define));
			ns.Add("if", TCLObject.func(if_define));

			ns.Add("expr", TCLObject.func(expr_do));

			//TODO: to stdlib
			ns.Add("clock", TCLObject.func(clock_get));


			ns.Add("rand", TCLObject.func(math_rand));
			ns.Add("round", TCLObject.func(math_round));

			ns.Add("return", TCLObject.func(break_return));

			ns.Add("incr", TCLObject.func( (TCLObject[] argv) => { return argv[0]+1; }  ));
		}

		string stringTcl(TCLObject cmd)
		{
			var list = evalTclOrString(cmd);

			string sout = "";

			if (list.oo is string)
				return list.oo as string;

			foreach (var s in list.oo as List<TCLObject>)
				sout += s;

			return sout;
		}


		TCLObject evalTclOrString(TCLObject cmd)
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

			List<TCLObject> _list = new List<TCLObject>();

			for (int i = 0; i < ssl; i++)
			{
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
						TCLObject kvar = null;// GLOBALS[arr['key']];
						var key = stringTcl(arr.val);

						//echo "kv 'var' 'key' ";

						_list.Add( kvar[key] );
					}
					else
						_list.Add( ns[arr.key] );

					i = arr.pos;
					sua = i;
					sul = 1;//string length is 1 (+breakked of var)

				}
				else if (ss[i] == '[')
				{

					var arr = new ReadVarResult();

					TCL.readSqure(ss, i, arr);

					_list.Add(evalTclScript(TCL.parseTCL(arr.key, false)) );//TODO:recursive

					i = arr.pos+1;//skip ]
					sua = i;
					//sul = 1;//string length is 1 (+breakked of var)
				}
				else
				{
					sul++;
				}

			}

			if (sul > 0)
				_list.Add( ss.Substring(sua, sul) );

			if( _list.Count == 1 )
				return _list[0];

			return TCLObject.auto( _list );
		}

		public static TCLInterp runningNow = null;
		public TCLObject returnValue;

		public TCLObject evalTclLine(TCLObject cmd)
		{
			runningNow = this;

			if ((cmd).is_array)
			{
				var cmdname = cmd[0].ToString();

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
					return erro_print_r(cmdname, true);
				}
			}

			return cmd;
		}

		public TCLObject commandCall( string cmdname, object[] argv )
		{
			var func = ns[cmdname];

			return func.Call((argv) as TCLObject[]);
		}

		public TCLObject[] cmd_argv(TCLObject cmd, int v)
		{
			int nn = cmd.Count - v;
			var _out = new TCLObject[nn];

			for (int i = 0; i < nn; i++)
			{
				int from = i + v;
				if (cmd[from].kind == TCLKind.evstring)
					_out[i] = stringTcl(cmd[from]);
				else if (cmd[from].oo is string)
				{
					var arg = cmd[from].oo as string;

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

		private TCLObject evalTclScript(List<TCLObject> list)
		{
			TCLObject res = null;

			foreach (var a in list)
			{
				res = evalTclLine(a);
			}

			return res;
		}

		private TCLObject erro_print_r(TCLObject func, bool v)
		{
			Console.WriteLine("error:" + func.ToString());

			return null;
		}


	}
}
