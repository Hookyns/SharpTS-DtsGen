using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharpTsDtsGen
{
	class Program
	{
		/// <summary>
		/// Main
		/// </summary>
		/// <param name="args">[ outPath, ...input assemblies ]</param>
		static int Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Invalid number of arguments." + Environment.NewLine + "Expected: outPath, ...input assemblies");
				return -1;
			}
			
			// Load assemblies
			List<Assembly> assemblies = args.Skip(1).Select(path => Assembly.LoadFrom(Path.GetFullPath(path))).ToList();

			GenerateOutputs(args.First(), assemblies);
			
			return 0;
		}

		/// <summary>
		/// Check if type implements namespace with given name
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private static bool ImplementsType(Type type, string name)
		{
			return type.GetInterfaces().Any(i => i.Name == name);
		}

		/// <summary>
		/// Generate outputs
		/// </summary>
		/// <param name="outputPath"></param>
		/// <param name="assemblies"></param>
		private static void GenerateOutputs(string outputPath, ICollection<Assembly> assemblies)
		{
			// Find all types from specified assemblies
			var assembliesTypes = assemblies.SelectMany(a => a.GetTypes()).ToList();
			
			// Find Pages
			IEnumerable<Type> pages = assembliesTypes.Where(t => ImplementsType(t, "IPage"));

			// Find ViewModels
			IEnumerable<Type> viewModels = assembliesTypes.Where(t => ImplementsType(t, "IViewModel"));
			
			
			var types = viewModels.Concat(pages);
			var interfaceGenerator = new InterfaceGenerator(assemblies.ToArray());
			
			foreach (var type in types)
			{
				interfaceGenerator.GenerateInterfaceOf(type);
			}
			
			// Create file
			File.WriteAllText(Path.Join(Path.GetFullPath(outputPath), "application.d.ts"), interfaceGenerator.GetDeclarations());
		}
	}
}