using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace TCLSHARP
{
    public class TclRuntimeAttribute : Attribute
    { 
    }

    public class NetObject : TCLObject
    {
        protected Type _class;

        public NetObject(object self) : base(self)
        {
            _class = self.GetType();
        }

        public override TCLAtom Command(ITCLInterp i, TCLAtom tCLObject)
        {
            var argv = i.cmd_argv(tCLObject,1);

            var ffs = _class.GetMember(argv[0]);

            var ff = ffs[0];

            if (ff is PropertyInfo)
            {
                (ff as PropertyInfo).SetValue(oo, argv[1].oo);
            }

            if (ff is MethodInfo)
            {
                var nobj = (ff as MethodInfo).Invoke( oo,  TclNetRuntime.toSimpleParamsArray(argv,1) );

                if( nobj != null )
                    return new NetObject(nobj);
            }
                

            return null;
        }

        protected MethodInfo _getProp = null;

        public override TCLAtom this[string index] 
        {
            get
            {
                if (_getProp == null)
                {
                    var all = _class.GetMethods();

                    foreach (var mi in all)
                    {
                        var pps = mi.GetParameters();
                        if ( pps != null & pps.Length > 0 && pps[0].ParameterType == typeof(string))
                        {
                            _getProp = mi;
                            break;
                        }
                    }
                }


                return TCLAtom.auto(_getProp.Invoke(oo , new object[] { index  } ));
            }

            set
            {
                _class.GetMethod("get_Item").Invoke(oo, new object[] { value });
            }
        }
    }

    public class NetClass : TCLAtom
    {
        protected Type _class;

        public delegate TCLAtom PerformCommand(TCLAtom[] argv);

        public NetClass(object oo) : base(oo, TCLKind.cmd)
        {
            _class = oo as Type;

            oo = (object)((PerformCommand)_DoCommand);
        }

        public override TCLAtom Command(ITCLInterp i, TCLAtom tCLObject)
        {
            if (tCLObject[1] == "new")
            {
                var obj = Activator.CreateInstance(_class, TclNetRuntime.toSimpleParamsArray(i.cmd_argv(tCLObject, 2)));

                return new NetObject(obj);
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        protected TCLAtom _DoCommand(TCLAtom[] argv)
        {
            return null;
        }

        public override TCLAtom this[string index] 
        {
            get
            {
                index = check_forbidden(index);

                var ffs = _class.GetMember(index);

                var ff = ffs[0];

                if (ff is MethodInfo)
                {
                    if( ff.GetCustomAttribute<TclRuntimeAttribute>() != null)
                        return TCLAtom.def_cmd( (argv) => { return TCLObject.auto((ff as MethodInfo).Invoke(null, new object[] { argv })); } );
                    else
                        return TCLAtom.func((argv) => { return TCLObject.auto((ff as MethodInfo).Invoke(null, new object[] { argv })); });
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
        public static TCLAtom extern_define(TCLAtom[] argv)
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
                        interp.ns["tclr::" + line[1]] = TCLAtom.func((argv) => { return TCLObject.auto((mm as MethodInfo).Invoke(null, null)); });
                    }
                    else
                    {
                        interp.ns["tclr::" + line[1]] = TCLAtom.func((argv) => { return TCLObject.auto((mm as PropertyInfo).GetValue(null)); });
                    }
                }
                else if (line[0] == "class")
                {
                    var fullname = line[1].ToString();

                    var qname = fullname.Split("::");

                    var typename = (string[])qname.Clone();
                    

                   


                    var tt = lockupCLRType(String.Join(".", typename));

                    interp.ns["tclr::" + line[1]] = new NetClass(tt );
                }
            }

            return null;
        }

        internal static object[] toSimpleParamsArray(TCLAtom tCLObject, int from = 0 )
        {
            var aparams = new object[tCLObject.Count-from];


            for (int i = from; i < aparams.Length; i++)
            {
                aparams[i] = tCLObject[from+i].oo;
            }

            return aparams;
        }
    }
}
