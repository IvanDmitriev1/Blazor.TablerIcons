using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Blazor.TablerIconsGenerator;

[Generator(LanguageNames.CSharp)]
public class IconGenerator : IIncrementalGenerator
{
	private const string Path = "Blazor.TablerIconsGenerator.node_modules._tabler.icons.icons.";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(Generate);
	}

	private static void Generate(IncrementalGeneratorPostInitializationContext context)
	{
		try
		{
			var assembly = Assembly.GetExecutingAssembly();
			var iconAssemblyNames = assembly.GetManifestResourceNames()
											.Where(static s => s.StartsWith(Path));

			const string projNamespace = "Blazor.TablerIcons";

			var enumSb = new StringBuilder();
			enumSb.AppendLine($"namespace {projNamespace};");
			enumSb.AppendLine("public enum TablerIconType");
			enumSb.AppendLine("{");

			var extensionsSb = new StringBuilder();
			extensionsSb.AppendLine($"namespace {projNamespace};");
			extensionsSb.AppendLine("public static partial class TablerIconTypeExtensions");
			extensionsSb.AppendLine("{");

			var extensionsMethodSb = new StringBuilder();
			extensionsMethodSb.AppendLine($"namespace {projNamespace};");
			extensionsMethodSb.AppendLine("public static partial class TablerIconTypeExtensions");
			extensionsMethodSb.AppendLine("{");

			extensionsMethodSb.AppendLine("""
										  private static Microsoft.AspNetCore.Components.RenderFragment CreateRenderFragment(string content) => builder =>
										  {
										  	int seq = 0;
										  
										  	builder.OpenElement(seq++, "svg");
										  
										  	builder.AddAttribute(seq++, "style", "width: 1em;");
										  	builder.AddAttribute(seq++, "focusable", false);
										  	builder.AddAttribute(seq++, "aria-hidden", true);
										  	builder.AddAttribute(seq++, "fill", "none");
										  	builder.AddAttribute(seq++, "stroke", "currentColor");
										  	builder.AddAttribute(seq++, "viewBox", "0 0 24 24");
										  	builder.AddAttribute(seq++, "stroke-width", "2");
										  	builder.AddAttribute(seq++, "stroke-linecap", "round");
										  	builder.AddAttribute(seq++, "stroke-linejoin", "round");
										  
										  	builder.AddMarkupContent(seq++, content);
										  
										  	builder.CloseElement();
										  };
										  """);

			extensionsMethodSb.AppendLine($"public static Microsoft.AspNetCore.Components.RenderFragment ToRenderFragment(this {projNamespace}.TablerIconType icon) => icon switch");
			extensionsMethodSb.AppendLine("{");

			foreach (var assemblyName in iconAssemblyNames)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				var iconFormattedName = FilterIconName(assemblyName);

				if (string.IsNullOrEmpty(iconFormattedName))
					continue;

				enumSb.Append(iconFormattedName);
				enumSb.Append(",\n");

				extensionsSb.AppendLine(CreateRenderFragmentField(assembly, assemblyName, iconFormattedName));
				extensionsMethodSb.AppendLine($"{projNamespace}.TablerIconType.{iconFormattedName} => {iconFormattedName},");
			}

			enumSb.Remove(enumSb.Length - 2, 2);

			enumSb.AppendLine("}");
			var enumSource = enumSb.ToString();
			context.AddSource("TablerIconType.g.cs", SourceText.From(enumSource, Encoding.UTF8));

			extensionsSb.AppendLine("}");
			var extensionsSource = extensionsSb.ToString();
			context.AddSource("TablerIconTypeExtensions.fields.g.cs", SourceText.From(extensionsSource, Encoding.UTF8));

			extensionsMethodSb.AppendLine("_ => throw new System.ArgumentOutOfRangeException(nameof(icon), icon, null)");
			extensionsMethodSb.AppendLine("};");
			extensionsMethodSb.AppendLine("}");
			var extensionMethodSource = extensionsMethodSb.ToString();
			context.AddSource("TablerIconTypeExtensions.methods.g.cs", SourceText.From(extensionMethodSource, Encoding.UTF8));
		}
		catch (OperationCanceledException ) { }
		catch (Exception e)
		{
			//TODO
		}
	}

	private static string FilterIconName(string iconName)
	{
		var iconNameSpan = iconName.AsSpan();

		iconNameSpan = iconNameSpan.Slice(Path.Length);
		iconNameSpan = iconNameSpan.Slice(0, iconNameSpan.Length - 4);

		if (char.IsDigit(iconNameSpan[0]))
		{
			return string.Empty;
		}

		int newSize = 0;
		Span<int> minusIndexes = stackalloc int[5];

		int g = 0;
		for (int i = 0; i < iconNameSpan.Length; i++)
		{
			var c = iconNameSpan[i];

			if (c != '-')
			{
				newSize++;
				continue;
			}

			minusIndexes[g] = i;
			g++;
		}

		Span<char> span = stackalloc char[newSize];
		span[0] = char.ToUpper(span[0]);
		int j = 0;

		foreach (var c in iconNameSpan)
		{
			if (c == '-')
			{
				continue;
			}

			span[j] = c;
			j++;
		}

		foreach (var index in minusIndexes)
		{
			if (span.Length <= index)
				continue;

			span[index] = char.ToUpper(span[index]);
		}

		return span.ToString();
	}

	private static string CreateRenderFragmentField(Assembly assembly, string iconAssemblyName, string iconName)
	{
		using var stream = assembly.GetManifestResourceStream(iconAssemblyName);
			
		var svg = new XmlDocument();
		svg.Load(stream);

		var content = svg.DocumentElement.InnerXml;

		var sb = new StringBuilder();
		sb.Append($"private const string {iconName}Content =");
		sb.Append('"');
		sb.Append('"');
		sb.Append('"');
		sb.Append(content);
		sb.Append('"');
		sb.Append('"');
		sb.Append('"');
		sb.Append(';');
		sb.AppendLine();

		string renderFragmentField =
			$"private static readonly Microsoft.AspNetCore.Components.RenderFragment {iconName} = CreateRenderFragment({iconName}Content);";

		sb.AppendLine(renderFragmentField);

		return sb.ToString();
	}
}