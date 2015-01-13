﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CLRSharp
{

    public class CLRSharp_Environment : ICLRSharp_Environment
    {
        public string version
        {
            get
            {
                return "0.31.5Alpha";
            }
        }
        public ICLRSharp_Logger logger
        {
            get;
            private set;
        }
        public CLRSharp_Environment(ICLRSharp_Logger logger)
        {
            this.logger = logger;
            logger.Log_Warning("CLR# Ver:" + version + " Inited.");
        }
        Dictionary<string, ICLRType> mapType = new Dictionary<string, ICLRType>();
        //public Dictionary<string, Mono.Cecil.ModuleDefinition> mapModule = new Dictionary<string, Mono.Cecil.ModuleDefinition>();

        public void LoadModule(System.IO.Stream dllStream, System.IO.Stream pdbStream)
        {
            var module = Mono.Cecil.ModuleDefinition.ReadModule(dllStream);
            if (pdbStream != null)
            {
                module.ReadSymbols(new Mono.Cecil.Pdb.PdbReaderProvider().GetSymbolReader(module, pdbStream));
            }
            //mapModule[module.Name] = module;
            if (module.HasTypes)
            {
                foreach (var t in module.Types)
                {
                    mapType[t.FullName] = new Type_Common_CLRSharp(this, t);
                }
            }
            if (module.HasAssemblyReferences)
            {
                foreach (var ar in module.AssemblyReferences)
                {
                    if (moduleref.Contains(ar.Name) == false)
                        moduleref.Add(ar.Name);
                }
            }
        }
        List<string> moduleref = new List<string>();
        public string[] GetAllTypes()
        {
            string[] array = new string[mapType.Count];
            mapType.Keys.CopyTo(array, 0);
            return array;
        }
        public string[] GetModuleRefNames()
        {
            return moduleref.ToArray();
        }
        //得到类型的时候应该得到模块内Type或者真实Type
        //一个统一的Type,然后根据具体情况调用两边

        public ICLRType GetType(string fullname)
        {
            ICLRType type = null;
            bool b = mapType.TryGetValue(fullname, out type);
            if (!b)
            {
                List<ICLRType> subTypes = new List<ICLRType>();
                if (fullname.Contains("<>") || fullname.Contains("/"))//匿名类型
                {
                    string[] subts = fullname.Split('/');
                    ICLRType ft = GetType(subts[0]);
                    if (ft is ICLRType_Sharp)
                    {
                        for (int i = 1; i < subts.Length; i++)
                        {
                            ft = ft.GetNestType(this, subts[i]);
                        }
                        return ft;
                    }
                }
                string fullnameT = fullname.Replace('/', '+');

                if (fullnameT.Contains("<"))
                {
                    string outname = "";
                    int depth = 0;
                    int lastsplitpos = 0;
                    for (int i = 0; i < fullname.Length; i++)
                    {
                        string checkname = null;
                        if (fullname[i] == '/')
                        {

                        }
                        else if (fullname[i] == '<')
                        {
                            if (i != 0)
                                depth++;
                            if (depth == 1)//
                            {
                                lastsplitpos = i;
                                outname += "[";
                                continue;
                            }

                        }
                        else if (fullname[i] == '>')
                        {
                            if (depth == 1)
                            {
                                checkname = fullnameT.Substring(lastsplitpos + 1, i - lastsplitpos - 1);
                                var subtype = GetType(checkname);
                                subTypes.Add(subtype);
                                if (subtype is ICLRType_Sharp) subtype = GetType(typeof(CLRSharp_Instance));
                                outname += "[" + subtype.FullNameWithAssembly + "]";
                                lastsplitpos = i;
                            }
                            //if(depth>0)
                            depth--;
                            if (depth == 0)
                            {
                                outname += "]";
                                continue;
                            }
                            else if (depth < 0)
                            {
                                depth = 0;
                            }
                        }
                        else if (fullname[i] == ',')
                        {
                            if (depth == 1)
                            {
                                checkname = fullnameT.Substring(lastsplitpos + 1, i - lastsplitpos - 1);
                                var subtype = GetType(checkname);
                                subTypes.Add(subtype);
                                if (subtype is ICLRType_Sharp) subtype = GetType(typeof(CLRSharp_Instance));
                                outname += "[" + subtype.FullNameWithAssembly + "],";
                                lastsplitpos = i;
                            }
                        }
                        if (depth == 0)
                        {
                            outname += fullnameT[i];
                        }
                    }
                    fullnameT = outname;
                    //    fullnameT = fullnameT.Replace('<', '[');
                    //fullnameT = fullnameT.Replace('>', ']');


                }

                System.Type t = System.Type.GetType(fullnameT);

                if (t == null)
                {

                    foreach (var rm in moduleref)
                    {
                        t = System.Type.GetType(fullnameT + "," + rm);
                        if (t != null)
                        {
                            fullnameT = fullnameT + "," + rm;
                            break;
                        }
                    }

                }
                if (t != null)
                {
                    type = new Type_Common_System(this, t, fullnameT, subTypes.ToArray());
                }
                mapType[fullname] = type;
            }
            return type;
        }


        public ICLRType GetType(System.Type systemType)
        {
            ICLRType type = null;
            bool b = mapType.TryGetValue(systemType.FullName, out type);
            if (!b)
            {
                type = new Type_Common_System(this, systemType, systemType.FullName, null);
            }
            return type;
        }
        public void RegType(ICLRType type)
        {
            mapType[type.FullName] = type;
        }
    }
}
