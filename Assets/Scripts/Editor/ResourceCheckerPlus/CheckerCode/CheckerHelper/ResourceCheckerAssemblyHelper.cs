using System;
using System.Collections.Generic;
//using UnityEngine;

namespace ResourceCheckerPlus
{
    /// <summary>
    /// 获取所有可能已加载的程序集中的资源，From DJCommon
    /// </summary>
    public class ResourceCheckerAssemblyHelper
    {
        private static readonly Dictionary<string, Type> s_CachedTypes = new Dictionary<string, Type>();
        private static readonly List<string> s_LoadedAssemblyNames = new List<string>();

        static ResourceCheckerAssemblyHelper()
        {
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                s_LoadedAssemblyNames.Add(assembly.FullName);
            }
        }

        public static string[] GetLoadedAssemblyNames()
        {
            return s_LoadedAssemblyNames.ToArray();
        }

        public static Type GetTypeWithinLoadedAssemblies(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new System.ArgumentException("Type name is invalid.");
            }

            Type type = null;
            if (s_CachedTypes.TryGetValue(typeName, out type))
            {
                return type;
            }

            type = Type.GetType(typeName);
            if (type != null)
            {
                s_CachedTypes.Add(typeName, type);
                return type;
            }

            foreach (string assemblyName in s_LoadedAssemblyNames)
            {
                type = Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));
                if (type != null)
                {
                    s_CachedTypes.Add(typeName, type);
                    return type;
                }
            }
            return null;
        }

        public static Type GetResourceCheckerType(string name)
        {
            var fullName = "ResourceCheckerPlus." + name;
            var type = GetTypeWithinLoadedAssemblies(fullName);
            if (type == null)
                type = GetTypeWithinLoadedAssemblies(name);
            //if (type == null)
            //    Debug.LogWarning("Class doesn't exist:" + name);
            return type;
        }
    }
}

