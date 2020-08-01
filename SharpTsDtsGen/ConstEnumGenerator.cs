using System.Collections.Generic;
using System.Text;

namespace SharpTsDtsGen
{
	/// <summary>
	/// Generator for key:value object as enum of constants
	/// </summary>
	public class ConstEnumGenerator
	{
		/// <summary>
		/// Generate const-enum object
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public string GetConstEnumOf(IDictionary<string, string> values)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("{");

			foreach (var item in values)
			{
				sb.AppendLine($"\t{item.Key}: \"{item.Value}\"");
			}
			
			sb.AppendLine("}");

			return sb.ToString();
		}
	}
}