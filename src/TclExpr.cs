using System;
using System.Collections.Generic;
using System.Text;

namespace TCLSHARP
{
	public class TCLLexer
	{
		protected string _str;

		int _i = 0;
		string _token = null;
		string _readyToken = null;

		public TCLLexer(string str)
		{
			_str = str;
		}

		public bool eof 
		{
			get
			{
				if (_readyToken != null)
					return false;

				return _str.Length <= _i;
			}

			set
			{ 
			}
		}

		public void returnToken()
		{
			_readyToken = _token;
		}

		static string pairable = "+-<>*!=";

		public bool getExpectedToken( string tok )
		{
			if (getToken() != tok)
			{
				returnToken();
				return false;
			}

			return true;
		}

		public string getToken()
		{
			if (_readyToken != null)
			{
				_token = _readyToken;
				_readyToken = null;

				return _token;
			}

			int ssl = (_str).Length;

			//skip begining
			bool skip = true;

			int keya = _i;
			int keyb = -1;

			//only a-z 0-1 _ ()
			for (; _i < ssl; _i++)
			{
				if (skip)
				{
					if (_str[_i] == ' ')
					{
						keya = _i+1;//in case of end
						continue;
					}

					keya = _i;
					skip = false;
					
				}

				if (_str[_i] == '$')
					continue;

				if (_i == keya && IsSymbol(_str[_i] ) )
				{
					var sym = _str[_i];

					if (_i + 1 < ssl && IsSymbol(_str[_i+1])  && pairable.Contains(sym) )
					{
						_i++;
					}

					keyb = _i+1;
					_i++;

					break;
				}

				//non ascii?
				if (_str[_i] > 127)
				{
					break;
				}
				else if (_str[_i] < 47)
				{
					keyb = _i;
					break;
				}

				
				
				if (!char.IsLetterOrDigit(_str[_i]))
				{
					keyb = _i;
					break;
				}
				
			}

			if (keyb == -1)
				keyb = ssl;

			_token = _str.Substring( keya, Math.Max( 0 , keyb-keya ) );

			return _token;
		}

		public static bool IsSymbol(char v)
		{
			if ( v > 32 && v <= 47 )
				return true;

			if (v >= 58 && v <= 64)
				return true;

			return false;
		}
	}

	public class TclOperator
	{
		public virtual object Do(TclExpr exp)
		{
			return null;
		}
	}

	public class TclCall : TclOperator
	{
		private string tok;
		private List<object> v;

		public TclCall(string tok, List<object> v)
		{
			this.tok = tok;
			this.v = v;
		}

		public override object Do(TclExpr exp)
		{
			
			return TCLInterp.runningNow.commandCall(tok, new TCLAtom[] { TCLObject.auto( exp.doStack(v)) } );
		}
	}

	public class TclExpr
    {
		TCLLexer lexer;
		TCLInterp tCLInterp;
		List<object> _flow;




		public object readOperand()
		{
			var tok = lexer.getToken();

			if (tok[0] != '$')
			{
				if (char.IsDigit(tok[0]))
					return int.Parse(tok);

				if( !lexer.getExpectedToken("(") )
					throw new Exception();

				List<object> subFlow = null;

				if (!lexer.getExpectedToken(")"))
				{
					subFlow = subExpr();

					if (!lexer.getExpectedToken(")"))
						throw new Exception();

					
				}

				return new TclCall(tok, subFlow);
			}

			return tok;
		}

		private List<object> subExpr()
		{
			var prew = _flow;
			
			_flow = new List<object>();

			_parseTCLexpr();


			var res = _flow;

			_flow = prew;

			return res;
		}

		public object doStack(List<object> stack)
		{
			if (stack == null)
				return null;

			return _doStack(stack);
		}

		private object _doStack(List<object> stack)
		{
			var opstack = new Stack<object>();

			for(int i=0; i< stack.Count; i++)
			{
				var sm = stack[i];

				var sms = sm as string;
				
				if (sm is TclOperator)
				{
					opstack.Push(((TclOperator)sm).Do(this));
					continue;
				}

				if (sms == null || sms[0] == '$' )
				{
					opstack.Push(sm);
					continue;
				}

				

				double a = 0;
				double b = 0;

				switch (sms)
				{
					case "*":
						{
							a = operand_d(opstack.Pop());
							b = operand_d(opstack.Pop());

							opstack.Push(b*a);
							break;
						}
					case "/":
						{
							a = operand_d(opstack.Pop());
							b = operand_d(opstack.Pop());

							opstack.Push(b / a);
							break;
						}
					case "-":
						{
							a = operand_d(opstack.Pop());
							b = operand_d(opstack.Pop());

							opstack.Push(b-a);
							break;
						}
					case "==":
						{
							a = operand_d(opstack.Pop());
							b = operand_d(opstack.Pop());

							opstack.Push(b == a);
							break;
						}
					case "!=":
						{
							a = operand_d(opstack.Pop());
							b = operand_d(opstack.Pop());

							opstack.Push(b != a);
							break;
						}
					case "<":
						{
							a = operand_d(opstack.Pop());
							b = operand_d(opstack.Pop());

							opstack.Push(b < a);
							break;
						}
					case ">":
						{
							a = operand_d(opstack.Pop());
							b = operand_d(opstack.Pop());

							opstack.Push(b > a);
							break;
						}
				}
			}

			return opstack.Pop();
		}

		private double operand_d(object v)
		{
			if (v is TCLAtom)
				return operand_d(((TCLAtom)v).oo);

			if (v is int)
				return (int)v;

			if (v is double)
				return (double)v;

			if (v is string)
			{
				var vs = (string)v;

				if( vs[0] == '$' )
					return operand_d(this.tCLInterp.ns[vs.Substring(1)] );

				return double.Parse(vs);
			}

			return 0;
		}

		public TCLAtom evalTCLexpr(string exprs, TCLInterp tCLInterp )
		{
			this.tCLInterp = tCLInterp;

			_flow = new List<object>();


			lexer = new TCLLexer(exprs);


			_parseTCLexpr();

			var res = _doStack(_flow);

			return new TCLAtom(res);

		}

		public List<object> parseTCLexpr(string exprs)
		{
			this.tCLInterp = tCLInterp;

			_flow = new List<object>();


			lexer = new TCLLexer(exprs);


			_parseTCLexpr();

			

			return _flow;

		}

		public List<object> _parseTCLexpr(   )
		{
			var _opstack = new Stack<string>();

			int nnop = 0;

			while (!lexer.eof)
			{
				

				var tr = lexer.getToken();

				if (tr.Length == 0)
				{
					break;
				}

				if (tr == ",")
				{
					lexer.returnToken();
					break;
				}

				if (tr == ")" && !_opstack.Contains("("))
				{
					lexer.returnToken();
					break;
				}

				if ( !TCLLexer.IsSymbol(tr[0]) || tr[0] == '$' )
				{
					lexer.returnToken();

					var op1 = readOperand();

					_flow.Add(op1);
					nnop++;

					continue;
				}

				if (nnop == 0)
				{
					switch (tr)
					{
						case "-":
							{
								tr = "-n";
								break;
							}
					}

					nnop = 1;

				}
				



				while (_opstack.Count > 0)
				{
					_opstack.Peek();

					break;
				}

				_opstack.Push(tr);

			}

			while (_opstack.Count > 0)
			{
				_flow.Add(_opstack.Pop());

				break;
			}


			return _flow;
        }

		
	}
}
