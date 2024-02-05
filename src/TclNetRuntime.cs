using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace TCLSHARP
{
    public class TclRuntimeAttribute : Attribute
    { 
    }

    public class NetClass : TCLObject
    {
        protected Type _class;

        public delegate TCLObject PerformCommand(TCLObject[] argv);

        public NetClass(object oo) : base(oo, TCLKind.cmd)
        {
            _class = oo as Type;

            oo = (object)((PerformCommand)_DoCommand);
        }

        public override TCLObject Command(ITCLInterp i, TCLObject tCLObject)
        {
            return base.Command(i, tCLObject);
        }

        protected TCLObject _DoCommand(TCLObject[] argv)
        {
            return null;
        }

        public override TCLObject this[string index] 
        {
            get
            {
                index = check_forbidden(index);

                var ffs = _class.GetMember(index);

                var ff = ffs[0];

                if (ff is MethodInfo)
                {
                    if( ff.GetCustomAttribute<TclRuntimeAttribute>() != null)
                        return TCLObject.def_cmd( (argv) => { return TCLObject.auto((ff as MethodInfo).Invoke(null, new object[] { argv })); } );
                    else
                        return TCLObject.func((argv) => { return TCLObject.auto((ff as MethodInfo).Invoke(null, new object[] { argv })); });
                }

                return base[index];
            }
            set => base[index] = value; 
        }

        private string check_forbidden(string index)
        {
            if (index == "extern")
                return index + "_define";

            return index;
        }
    }
    public class TclNetRuntime
    {
        static public Type lockupCLRType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        [TclRuntimeAttribute]
        public static TCLObject extern_define(TCLObject[] argv)
        {
            var app = TCL.parseTCL(argv[1]);
            var interp = TCLInterp.runningNow;

            foreach (var line in app)
            {
                if (!line.is_array)
                    break;

                if (line[0] == "proc")
                {
                    var fullname = line[1].ToString();

                    var qname = fullname.Split("::");

                    var typename = (string[])qname.Clone();
                    var methodName = qname[qname.Length - 1];

                    Array.Resize(ref typename, qname.Length - 1);


                    var tt = lockupCLRType(String.Join(".", typename));

                    var mm = tt.GetMember(methodName)[0];

                    if (mm is System.Reflection.MethodInfo)
                    {
                        interp.ns["tclr::" + line[1]] = TCLObject.func((argv) => { return TCLObject.auto((mm as MethodInfo).Invoke(null, null)); });
                    }
                    else
                    {
                        interp.ns["tclr::" + line[1]] = TCLObject.func((argv) => { return TCLObject.auto((mm as PropertyInfo).GetValue(null)); });
                    }
                }
            }

            return null;
        }
    }
}
