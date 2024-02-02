﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TCLSHARP
{

	public class ReadVarResult
	{
		public int pos;
		public string key;
		public string val;
	}

	public enum TCLKind : int
	{
		Any = 0,
		cmd = 1,//command line like cmd

		//squreBrakets = 2,
		func = 3,//proc
		evstring = 4,
		
	}

	public class TCLObject
	{
		public static TCLObject nil = new TCLObject(null);
		public static TCLObject bool_true = new TCLObject(true);

		public object oo;
		public TCLKind kind;


		public bool is_string 
		{
			get
			{
				return kind == TCLKind.evstring;
			}
		}

		public bool is_array 
		{
			get
			{
				if (oo is Array)
					return true;

				if (oo is List<TCLObject>)
					return true;

				return false;
			}
		}

		internal TCLObject[] Slice( int v, int nn = int.MaxValue)
		{
			var cmd = this;

			if (oo is TCLObject[] && v == 0)
				return oo as TCLObject[];

			if (v < 0)
				return null;

			int ann = Math.Min(nn, cmd.Count) - v;

			var arr = new TCLObject[ann];

			for (int i = 0; i < ann; i++)
				arr[i] = cmd[v + i];

			return arr;
		}

		public int Count 
		{
			get
			{
				if (oo is TCLObject[])
					return ((TCLObject[])(oo)).Length;

				if (oo is List<TCLObject>)
					return ((List<TCLObject>)(oo)).Count;

				return 1;
			}
		}

		public TCLObject(object oo, TCLKind kind = TCLKind.Any)
		{
			this.oo = oo;
			this.kind = kind;
		}

		public static implicit operator TCLObject(string value)
		{
			

			return new TCLObject( value );
		}

		public static implicit operator string(TCLObject value)
		{
			return value.ToString();
		}

		public static implicit operator int(TCLObject value)
		{
			if (value.oo is int)
				return (int)value.oo; 
			
			if (value.oo is double)
				return (int)((double)value.oo);

			return int.Parse(value.ToString());
		}

		public static implicit operator bool(TCLObject value)
		{
			if (value.oo is bool)
				return ((bool)value.oo);

			if (value.oo is int)
				return ((int)value.oo)!=0;

			return bool.Parse(value.ToString());
		}

		public static implicit operator TCLObject(TCLObject[] value)
		{


			return new TCLObject(value);
		}

		public static implicit operator TCLObject( List<TCLObject> value)
		{


			return new TCLObject(value);
		}

		public static implicit operator TCLObject[](TCLObject value)
		{
			return value.oo as TCLObject[];
		}



		public TCLObject this[int index]
		{
			get
			{
				if (oo is List<TCLObject>)
				{
					return (oo as List<TCLObject>)[index];
				}

				if (oo is TCLObject[])
				{
					return (oo as TCLObject[])[index];
				}

				return null;
			}
		}

		public TCLObject this[string index]
		{
			get
			{
				return null;
			}
		}

		internal static TCLObject auto(object tok)
		{
			if (tok is TCLObject)
				return tok as TCLObject;

			return new TCLObject(tok);
		}

		public override string ToString()
		{
			return oo.ToString();
		}

		public TCLObject Call(TCLObject[] tCLObject)
		{
			Func<TCLObject[], TCLObject> pfunc = oo as Func<TCLObject[], TCLObject>;

			if(pfunc != null )
				return pfunc(tCLObject);

			return null;
		}

		public TCLObject Command(ITCLInterp i,TCLObject tCLObject)
		{
			if (kind == TCLKind.cmd)
			{
				Func<TCLObject[], TCLObject> pfunc = oo as Func<TCLObject[], TCLObject>;

				if (pfunc != null)
					return pfunc( i.cmd_argv(tCLObject, 0) );
			}

			var argv = i.cmd_argv(tCLObject, 1);

			return Call(argv);
		}

		public static TCLObject func(Func<TCLObject[], TCLObject> pfunc)
		{
			return new TCLObject(pfunc, TCLKind.func);
		}
		public static TCLObject def_cmd(Func<TCLObject[], TCLObject> pfunc)
		{
			return new TCLObject(pfunc, TCLKind.cmd);
		}
	}

	public interface ITCLInterp
	{
		TCLObject[] cmd_argv(TCLObject argv, int v);
	}

	class TCL
	{



		public static int readSqure(string ss, int i, ReadVarResult return_array = null)
		{

			i++;
			int ssl = (ss).Length;

			int keya = i;
			int keyb = -1;

			int deep = 0;

			
			//only a-z 0-1 _ ()
			for (; i < ssl; i++)
			{
				int ssi = (ss[i]);

				//echo "tok (round)".ss[i].";";

				// ()
				if (ssi == '[')
				{
					deep++;
					continue;
				}

				if (ssi == ']')
				{
					deep--;

					if( deep >= 0 )
						continue;

					keyb = i;
					break;
				}

			}

			if (return_array != null)
			{
				ReadVarResult arr = return_array;



				arr.pos = i;
				arr.key = ss.Substring(keya, keyb - keya);

			}

			return i;
		}

		public static int readVariable( string ss, int i , ReadVarResult return_array = null)
	{

		i++;
		int ssl = (ss).Length;

		int keya = i;
		int keyb = -1;

		bool round = false;
		//only a-z 0-1 _ ()
		for (; i < ssl; i++)
		{
			int ssi = (ss[i]);

			//echo "tok (round)".ss[i].";";

			// ()
			if (ssi == '(')
			{
				keyb = i;
				round = true;
				continue;
			}

			if (round)
			{
				if (ssi == 32)
				{
					continue;
				}
				else if(ssi == ')')
				{
					round = false;
					continue;
				}
				else if(ssi == '$')

				{
                    i = TCL.readVariable( ss, i, null);
				}
				//FIXME: else continue
			}

			char ci = ss[i];
			//any number or digit
			if (!(char.IsLetterOrDigit(ci) || (ci == '_')) )
			{
				break;
			}
		}

		string has_inner = null;
		if ( keyb == -1 )
		    keyb = i;
		else if ( return_array != null )
			has_inner = ss.Substring(keyb + 1,i -keyb - 1);

		if ( return_array != null )
        {
				ReadVarResult arr = return_array;
				
				 

					arr.pos = i;
					arr.key = ss.Substring(keya, keyb - keya);


			

			//echo "readVariable arr[key]";

			if (has_inner!= null)
                arr.val = has_inner;

		
		}

		return i;
	}

	public static List<TCLObject> parseTCL( string tcl, bool recurcive = false)
	{
		var dataArray = new List<TCLObject>();

			string ss = tcl;

		int sa = 0;

		var line = new List<TCLObject>();

		int ssl = (ss).Length;

			//echo "parceTCL ".trim(tcl)."\n";

			object tok;

		for (int i = 0; i < ssl; i++)
		{
			int ssi = (ss[i]);
			//echo "tok: <ssi> ".ss[i]."\n";

			if (ss[i] == '\n' || ss[i] == ';')
			{
				//echo 'newline? '.trim( substr( ss, i, 5 ) )."\n";

				if ((line).Count>0)
				{
						dataArray.Add((TCLObject)line);
						line = new List<TCLObject>();
					
				}

				continue;
			}

			if (ssi <= 32)
				continue;

			//echo "nws!";

			sa = i;

				//FIXME: ' - not tcl string
			if (ssi == '"' || ssi == '\'' || ssi == 123)
			{
				int starter = ssi;
					int breaker;
				if (ssi >= 91)
					breaker = ssi + 2;
				else
					breaker = ssi;

				//echo "brakets starter breaker \n";

				i++;
				int level = 0;

				for (; i < ssl; i++)
				{
					ssi = (ss[i]);

					if (ssi == starter && ssi >= 91)
					{
						level++;
						continue;
					}

					if (ssi == breaker)
					{
						level--;

						if (level < 0)
							break;
					}
				}

				tok = ss.Substring( sa + 1, i - sa - 1);

				if (starter == '"')
						tok = new TCLObject(tok, TCLKind.evstring );//string ""
				else if(starter == '{')
					tok = recurcive? TCL.parseTCL(tok.ToString()) : tok;

				goto finalTok;
			}

			//string token

			for (; i < ssl; i++)
			{
				ssi = (ss[i]);

				if (ssi <= 32)
					break;

				// " '
				if (ssi == 34 || ssi == 39)
					break;

				//
				if (ssi == '$')
				{
					i = TCL.readVariable( ss, i, null );

					//string token end - we merger all strings together
					continue;
				}

				if (ssi == '[')
				{
					i = TCL.readSqure(ss, i, null);

					//string token end - we merger all strings together
					continue;
				}

					// ; [] {}
					if (ssi == 59 || ssi == 91 || ssi == 93 || ssi == 123 || ssi == 125)
				{
					break;
					}
					
					
					
				}

				if (i > ss.Length)
					i = ss.Length;

			tok = ss.Substring( sa, i - sa);
			i--;

			if ( tok is string toks && Char.IsDigit(toks[0]))
			{
				if (toks.Length> 2 &&  toks[1] == 'x')
				{
					tok = hexdec(toks);
				}
				else
				{
						
					float tokf = floatval(toks);

						int toki = (int)tokf;
						
						

					if (toks == toki.ToString() )
						tok = toki;
					else if (tokf != 0 || toks == "0" )
							tok = toki;

					}
			}

		//echo "token? 'tok' \n";

		finalTok:

			line.Add( TCLObject.auto(tok) );
		}

		if ((line).Count > 0)
			dataArray.Add( line );


		return dataArray;
	}

		private static int hexdec(string tok)
		{
			return Convert.ToInt32(tok, 16); 
		}

		private static float floatval(string tok)
		{
			float tokf = 0;

			float.TryParse(tok,out tokf);

			return tokf;
		}




	}








}
