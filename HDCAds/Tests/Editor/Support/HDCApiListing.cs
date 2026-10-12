using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace HDC.Ads.Tests
{
    internal static class HDCApiListing
    {
        internal const string Indent = "  ";

        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, string> Keywords = new Dictionary<Type, string>
        {
            { typeof(void), "void" }, { typeof(object), "object" }, { typeof(string), "string" }, { typeof(bool), "bool" },
            { typeof(byte), "byte" }, { typeof(sbyte), "sbyte" }, { typeof(char), "char" }, { typeof(short), "short" },
            { typeof(ushort), "ushort" }, { typeof(int), "int" }, { typeof(uint), "uint" }, { typeof(long), "long" },
            { typeof(ulong), "ulong" }, { typeof(float), "float" }, { typeof(double), "double" }, { typeof(decimal), "decimal" },
        };

        internal static IEnumerable<string> Lines(Assembly assembly)
        {
            foreach (Type type in assembly.GetExportedTypes().OrderBy(type => Name(type), StringComparer.Ordinal))
            {
                yield return Header(type);
                foreach (string member in Members(type).OrderBy(member => member, StringComparer.Ordinal))
                    yield return Indent + member;
            }
        }

        private static string Header(Type type)
        {
            string kind;
            var bases = new List<string>();
            if (type.IsEnum)
            {
                kind = "enum";
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying != typeof(int))
                    bases.Add(Name(underlying));
            }
            else if (type.IsInterface)
                kind = "interface";
            else if (type.IsValueType)
                kind = "struct";
            else if (typeof(Delegate).IsAssignableFrom(type))
                kind = "delegate";
            else
            {
                kind = type.IsAbstract && type.IsSealed ? "static class"
                    : type.IsAbstract ? "abstract class"
                    : type.IsSealed ? "sealed class"
                    : "class";
                if (type.BaseType != null && type.BaseType != typeof(object))
                    bases.Add(Name(type.BaseType));
            }

            if (kind != "enum" && kind != "delegate")
                bases.AddRange(type.GetInterfaces().Where(Visible).Select(Name).OrderBy(name => name, StringComparer.Ordinal));
            return kind + " " + Name(type) + (bases.Count > 0 ? " : " + string.Join(", ", bases) : "");
        }

        private static IEnumerable<string> Members(Type type)
        {
            if (type.IsEnum)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    yield return field.Name + " = " + Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture);
                yield break;
            }

            if (typeof(Delegate).IsAssignableFrom(type))
            {
                MethodInfo invoke = type.GetMethod("Invoke");
                yield return Name(invoke.ReturnType) + " Invoke(" + Parameters(invoke) + ")";
                yield break;
            }

            bool inheritable = !type.IsSealed;
            foreach (MemberInfo member in type.GetMembers(Declared))
            {
                if (member.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    continue;
                switch (member)
                {
                    case ConstructorInfo constructor when !constructor.IsStatic && Visible(constructor, inheritable):
                        yield return Access(constructor) + "ctor(" + Parameters(constructor) + ")";
                        break;
                    case MethodInfo method when Visible(method, inheritable) && (!method.IsSpecialName || method.Name.StartsWith("op_")):
                        yield return Access(method) + Modifiers(method) + Name(method.ReturnType) + " " + method.Name
                            + TypeArguments(method) + "(" + Parameters(method) + ")";
                        break;
                    case PropertyInfo property:
                        string accessors = Accessor("get", property.GetMethod, inheritable) + Accessor("set", property.SetMethod, inheritable);
                        if (accessors.Length == 0)
                            break;
                        MethodInfo any = property.GetMethod ?? property.SetMethod;
                        ParameterInfo[] index = property.GetIndexParameters();
                        string name = index.Length == 0 ? property.Name : "this[" + Parameters(index) + "]";
                        yield return Modifiers(any) + "property " + Name(property.PropertyType) + " " + name + " {" + accessors + " }";
                        break;
                    case EventInfo @event when Visible(@event.AddMethod, inheritable):
                        yield return Static(@event.AddMethod) + "event " + Name(@event.EventHandlerType) + " " + @event.Name;
                        break;
                    case FieldInfo field when Visible(field, inheritable):
                        string what = field.IsLiteral ? "const " : (field.IsStatic ? "static " : "") + (field.IsInitOnly ? "readonly " : "");
                        string value = field.IsLiteral ? " = " + Value(field.GetRawConstantValue()) : "";
                        yield return Access(field) + what + "field " + Name(field.FieldType) + " " + field.Name + value;
                        break;
                }
            }
        }

        private static bool Visible(Type type) => type.IsPublic || type.IsNestedPublic;

        private static bool Visible(MethodBase method, bool inheritable) =>
            method != null && (method.IsPublic || (inheritable && (method.IsFamily || method.IsFamilyOrAssembly)));

        private static bool Visible(FieldInfo field, bool inheritable) =>
            field.IsPublic || (inheritable && (field.IsFamily || field.IsFamilyOrAssembly));

        private static string Access(MethodBase method) => method.IsPublic ? "" : "protected ";

        private static string Access(FieldInfo field) => field.IsPublic ? "" : "protected ";

        private static string Static(MethodBase method) => method.IsStatic ? "static " : "";

        private static string Accessor(string name, MethodInfo accessor, bool inheritable) =>
            Visible(accessor, inheritable) ? " " + Access(accessor) + name + ";" : "";

        private static string Modifiers(MethodInfo method)
        {
            string modifiers = Static(method);
            if (method.DeclaringType != null && method.DeclaringType.IsInterface)
                return modifiers;
            if (method.IsAbstract)
                return modifiers + "abstract ";
            if (!method.IsVirtual || method.IsFinal)
                return modifiers;
            return modifiers + (method.GetBaseDefinition().DeclaringType != method.DeclaringType ? "override " : "virtual ");
        }

        private static string TypeArguments(MethodInfo method) =>
            method.IsGenericMethodDefinition ? "<" + string.Join(", ", method.GetGenericArguments().Select(Name)) + ">" : "";

        private static string Parameters(MethodBase method)
        {
            string parameters = Parameters(method.GetParameters());
            return method.IsDefined(typeof(ExtensionAttribute), false) ? "this " + parameters : parameters;
        }

        private static string Parameters(IEnumerable<ParameterInfo> parameters) =>
            string.Join(", ", parameters.Select(parameter =>
            {
                var text = new StringBuilder();
                if (parameter.IsDefined(typeof(ParamArrayAttribute), false))
                    text.Append("params ");
                if (parameter.ParameterType.IsByRef)
                    text.Append(parameter.IsOut ? "out " : "ref ");
                text.Append(Name(parameter.ParameterType)).Append(' ').Append(parameter.Name);
                if (parameter.HasDefaultValue)
                    text.Append(" = ").Append(Value(parameter.DefaultValue));
                return text.ToString();
            }));

        private static string Value(object value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string text:
                    return "\"" + text + "\"";
                case bool flag:
                    return flag ? "true" : "false";
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        internal static string Name(Type type)
        {
            if (type.IsByRef)
                return Name(type.GetElementType());
            if (type.IsArray)
                return Name(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            if (type.IsGenericParameter)
                return type.Name;
            Type nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
                return Name(nullable) + "?";
            if (Keywords.TryGetValue(type, out string keyword))
                return keyword;

            string name = type.Name;
            int tick = name.IndexOf('`');
            if (tick >= 0)
                name = name.Substring(0, tick);
            if (type.IsGenericType)
                name += "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
            if (type.IsNested)
                return Name(type.DeclaringType) + "." + name;
            return string.IsNullOrEmpty(type.Namespace) ? name : type.Namespace + "." + name;
        }
    }
}
