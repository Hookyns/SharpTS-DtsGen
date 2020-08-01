using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpTsDtsGen
{
	/// <summary>
	/// Generator of type's interface/schema as d.ts interface
	/// </summary>
	public class InterfaceGenerator
	{
		/// <summary>
		/// Ignored method names
		/// </summary>
		private static string[] IgnoredMethods = {"GetHashCode", "Equals", "ToString", "Dispose", "GetType"};

		/// <summary>
		/// Context
		/// </summary>
		private readonly Assembly[] assemblies;

		/// <summary>
		/// Set of types from context located in generated interfaces
		/// </summary>
		private readonly HashSet<Type> dependentTypes = new HashSet<Type>();

		/// <summary>
		/// Interfaces mapped to namespaces
		/// </summary>
		private readonly Dictionary<string, List<string>> interfacesPerNamespaces =
			new Dictionary<string, List<string>>();

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="assemblies"></param>
		public InterfaceGenerator(Assembly[] assemblies)
		{
			this.assemblies = assemblies;
		}

		/// <summary>
		/// Generate interface
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public void GenerateInterfaceOf(Type type)
		{
			// Type already generated
			if (this.dependentTypes.Contains(type))
			{
				return;
			}

			// Public readonly fields
			IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Where(f => f.IsInitOnly);

			// Public virtual properties
			IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				; //.Where(t => t.GetGetMethod().IsVirtual);

			// Public methods
			IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(t => !t.IsSpecialName && !IgnoredMethods.Contains(t.Name));

			// Generate interface
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"\tinterface {type.Name} {{");

			foreach (var item in fields)
			{
				sb.AppendLine($"\t\t{item.Name}: {this.GetJsType(item.FieldType)};");
			}

			foreach (var item in properties)
			{
				sb.AppendLine(
					$"\t\t{(item.GetSetMethod(false) == null ? "readonly " : "")}{this.LcFirst(item.Name)}: {this.GetJsType(item.PropertyType)};");
			}

			foreach (var item in methods)
			{
				sb.AppendLine($"\t\t{this.LcFirst(item.Name)}(): Promise<{this.GetJsType(item.ReturnType)}>;");
			}

			sb.AppendLine("\t}");

			this.AddToNamespace(type.Namespace, sb.ToString());
		}

		public string GetDeclarations()
		{
			StringBuilder sb = new StringBuilder();

			foreach (var ns in this.interfacesPerNamespaces)
			{
				sb.AppendLine($"declare namespace {ns.Key} {{");

				foreach (var interfaceDeclaration in ns.Value)
				{
					sb.AppendLine(interfaceDeclaration);
				}

				sb.AppendLine("}");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Add declaration to namespace
		/// </summary>
		/// <param name="namespaceName"></param>
		/// <param name="declaration"></param>
		private void AddToNamespace(string namespaceName, string declaration)
		{
			List<string> interfaces;
			if (!this.interfacesPerNamespaces.TryGetValue(namespaceName, out interfaces))
			{
				interfaces = new List<string>();
				this.interfacesPerNamespaces.Add(namespaceName, interfaces);
			}

			interfaces.Add(declaration);
		}

		/// <summary>
		/// Lower-case first character
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private string LcFirst(string name)
		{
			return name.Substring(0, 1).ToLower() + name.Substring(1);
		}

		/// <summary>
		/// Get JS equivalent of C# type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private string GetJsType(Type type)
		{
			Type nullableUnderlyingType = Nullable.GetUnderlyingType(type);

			if (nullableUnderlyingType != null)
			{
				return this.GetJsType(nullableUnderlyingType) + " | null";
			}
			
			if (this.IsNumber(type))
			{
				return "number";
			}

			if (type == typeof(string))
			{
				return "string | null";
			}

			if (type == typeof(DateTime))
			{
				return "Date";
			}

			if (type == typeof(bool))
			{
				return "boolean";
			}

			if (type.GetInterfaces().Any(i => i == typeof(IDictionary)) && type.IsGenericType)
			{
				var typeArgs = type.GetGenericArguments();
				return $"{{ [key: {this.GetJsType(typeArgs[0])}]: {this.GetJsType(typeArgs[1])} }} | null";
			}

			if (type.GetInterfaces().Any(i => i == typeof(IEnumerable)) && type.IsGenericType)
			{
				return $"Array<{this.GetJsType(type.GetGenericArguments().First())}> | null";
			}

			if (this.assemblies.Contains(type.Assembly))
			{
				this.GenerateInterfaceOf(type);
				this.dependentTypes.Add(type);

				return type.FullName + " | null";
			}

			return "any";
		}

		/// <summary>
		/// Check if type is number
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private bool IsNumber(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
			}

			return false;
		}
	}
}