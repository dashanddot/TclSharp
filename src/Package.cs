using System;
using System.Collections.Generic;
using System.Text;

namespace TCLSHARP
{
    class Package
    {
        static Dictionary<string, TCLAtom> _provided = new Dictionary<string, TCLAtom>();
        static Dictionary<string, TCLAtom> _located = new Dictionary<string, TCLAtom>();

        internal static TCLAtom cmd_package(TCLAtom[] arg)
        {
            switch (arg[0].ToString())
            {
                case "provide":
                    {
                        //if version not set - return version
                        if (arg.Length <= 2)
                            return TCLAtom.auto( _provided.ContainsKey(arg[1]) ? 1 : 0 );

                        _provided[arg[1]] = new TCLObject( null, TCLKind.Any );

                        break;
                    }
                
                    //function to call if required
                case "ifneeded":
                    {
                        var procObj = TCLInterp.runningNow.creteProcedure(null, null, TCL.parseTCL(arg[3]) );

                        _located[arg[1]] = procObj;

                        break;
                    }
                case "require":
                    {
                        if (_provided.ContainsKey(arg[1]))
                        {
                            return TCLAtom.auto(true);
                        }
                        else
                        {
                            if (_located.ContainsKey(arg[1]))
                            {
                                _located[arg[1]].Call(new TCLAtom[0]);

                                return null;
                            }

                                throw new Exception();
                        }

                        break;
                    }
            }

            return null;
        }
    }
}
