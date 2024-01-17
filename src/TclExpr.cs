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

		static string pairable = "+-<>*";

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
						continue;

					skip = false;
					keya = _i;
				}

				if (_str[_i] == '$')
					continue;

				if (_i == keya && _str[_i] < 47)
				{
					var sym = _str[_i];

					if (_i + 1 < ssl && !char.IsLetterOrDigit(_str[_i+1])  && pairable.Contains(sym) )
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
				keyb = ssl - 1;

			_token = _str.Substring( keya, Math.Max( 0 , keyb-keya ) );

			return _token;
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

				if (!lexer.getExpectedToken(")"))
				{
					subExpr();

					if (!lexer.getExpectedToken(")"))
						throw new Exception();
				}

				return this.tCLInterp.commandCall( tok, null ).oo;
			}

			return tok;
		}

		private object subExpr()
		{
			var prew = _flow;
			
			_flow = new List<object>();

			_parseTCLexpr();

			var res = _doStack(_flow);


			_flow = prew;

			return res;
		}

		private object _doStack(List<object> stack)
		{
			var opstack = new Stack<object>();

			for(int i=0; i<_flow.Count; i++)
			{
				var sm = _flow[i];

				var sms = sm as string;

				if (sms == null)
				{
					opstack.Push(sm);
					continue;
				}

				switch (sms)
				{
					case "*":
						{
							opstack.Pop();
							opstack.Pop();
							opstack.Push(0);
							break;
						}
				}
			}

			return opstack.Pop();
		}

		public TCLObject parseTCLexpr(string exprs, TCLInterp tCLInterp )
		{
			this.tCLInterp = tCLInterp;

			_flow = new List<object>();


			lexer = new TCLLexer(exprs);


			_parseTCLexpr();

			var res = _doStack(_flow);

			return new TCLObject(res);

		}

		public object _parseTCLexpr(   )
		{
			var _opstack = new Stack<string>();

			int nnop = 0;

			while (!lexer.eof)
			{
				

				var tr = lexer.getToken();

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

				if (tr[0] > 47)
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


			return null;
        }

		
	}
}
