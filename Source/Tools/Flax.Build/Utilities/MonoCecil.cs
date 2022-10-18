// Copyright (c) 2012-2022 Wojciech Figat. All rights reserved.

using System;
using System.Linq;
using Mono.Cecil;

namespace Flax.Build
{
    /// <summary>
    /// The utilities for Mono.Cecil library usage.
    /// </summary>
    internal static class MonoCecil
    {
        public static bool HasAttribute(this TypeDefinition type, string fullName)
        {
            return type.CustomAttributes.Any(x => x.AttributeType.FullName == fullName);
        }

        public static bool HasAttribute(this FieldDefinition type, string fullName)
        {
            return type.CustomAttributes.Any(x => x.AttributeType.FullName == fullName);
        }

        public static bool HasInterface(this TypeDefinition type, string fullName)
        {
            return type.Interfaces.Any(x => x.InterfaceType.FullName == fullName);
        }

        public static bool HasMethod(this TypeDefinition type, string name)
        {
            return type.Methods.Any(x => x.Name == name);
        }

        public static MethodDefinition GetMethod(this TypeDefinition type, string name)
        {
            return type.Methods.First(x => x.Name == name);
        }

        public static MethodDefinition GetMethod(this TypeDefinition type, string name, int argCount)
        {
            return type.Methods.First(x => x.Name == name && x.Parameters.Count == argCount);
        }

        public static FieldDefinition GetField(this TypeDefinition type, string name)
        {
            return type.Fields.First(x => x.Name == name);
        }

        public static bool IsScriptingObject(this TypeDefinition type)
        {
            if (type.FullName == "FlaxEngine.Object")
                return true;
            return type.BaseType.IsScriptingObject();
        }

        public static bool IsScriptingObject(this TypeReference type)
        {
            if (type == null)
                return false;
            if (type.FullName == "FlaxEngine.Object")
                return true;
            try
            {
                return type.Resolve().IsScriptingObject();
            }
            catch
            {
                return false;
            }
        }

        public static bool CanBeResolved(this TypeReference type)
        {
            while (type != null)
            {
                if (type.Scope.Name == "Windows")
                    return false;
                if (type.Scope.Name == "mscorlib")
                    return type.Resolve() != null;

                try
                {
                    type = type.Resolve().BaseType;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        public static MethodReference InflateGeneric(this MethodReference generic, TypeReference T)
        {
            var instance = new GenericInstanceMethod(generic);
            instance.GenericArguments.Add(T);
            return instance;
        }

        public static void GetType(this ModuleDefinition module, string fullName, out TypeReference type)
        {
            if (!module.TryGetTypeReference(fullName, out type))
            {
                // Do manual search
                foreach (var a in module.AssemblyReferences)
                {
                    var assembly = module.AssemblyResolver.Resolve(a);
                    if (assembly == null || assembly.MainModule == null)
                        continue;
                    foreach (var t in assembly.MainModule.Types)
                    {
                        if (t.FindTypeWithin(fullName, out type))
                        {
                            module.ImportReference(type);
                            return;
                        }
                    }
                }

                throw new Exception($"Failed to find type {fullName} from module {module.FileName}.");
            }
        }

        private static bool FindTypeWithin(this TypeDefinition t, string fullName, out TypeReference type)
        {
            if (t.FullName == fullName)
            {
                type = t;
                return true;
            }
            foreach (var n in t.NestedTypes)
            {
                if (n.FindTypeWithin(fullName, out type))
                    return true;
            }
            type = null;
            return false;
        }
    }
}
