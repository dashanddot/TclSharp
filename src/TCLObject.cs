namespace TCLSHARP
{
	//root object
	//root object
	public class TCLObject : TCLAtom
	{
		public TCLObject(object oo, TCLKind kind = TCLKind.Any) : base(oo, kind)
		{
		}

		public static TCLAtom auto(object tok)
		{
			if (tok is TCLAtom)
				return tok as TCLAtom;

			return new TCLAtom(tok);
		}
	}

}
