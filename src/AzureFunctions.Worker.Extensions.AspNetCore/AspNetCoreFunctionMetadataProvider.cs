using System.Collections.Frozen;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace AzureFunctions.Worker.Extensions.AspNetCore;

/// <summary>
/// Wraps built-in <see cref="IFunctionMetadataProvider"/> provider to return custom <see cref="AspNetCoreFunctionMetadata"/>.
/// </summary>
/// <param name="functionMetadataProvider">Built-in <see cref="IFunctionMetadataProvider"/></param>
public sealed partial class AspNetCoreFunctionMetadataProvider(IFunctionMetadataProvider functionMetadataProvider)
{
    private FrozenDictionary<string, AspNetCoreFunctionMetadata>? metadata;

    public FrozenDictionary<string, AspNetCoreFunctionMetadata> Metadata => this.metadata ??= this.InitializeMetadata();

    public AspNetCoreFunctionMetadata GetFunctionMetadata(string functionName)
    {
        return this.Metadata[functionName];
    }

    private static Type? TryUnwrapDataType(Type type)
    {
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() is { } genericTypeDefinition &&
            (genericTypeDefinition == typeof(Task<>) ||
            genericTypeDefinition == typeof(ValueTask<>)))
        {
            return type.GetGenericArguments()[0];
        }
        else if (type == typeof(Task) || type == typeof(ValueTask) || type == typeof(void))
        {
            return null;
        }

        return type;
    }

    [GeneratedRegex("^(?<typeName>.*)\\.(?<methodName>\\S*)$")]
    private static partial Regex EntryPointRegex();

    private FrozenDictionary<string, AspNetCoreFunctionMetadata> InitializeMetadata()
    {
        var scriptRoot = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY")!;

        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
        };

        return functionMetadataProvider
            .GetFunctionMetadataAsync(scriptRoot)
            .GetAwaiter()
            .GetResult()
            .GroupBy(metadata => Path.Combine(scriptRoot, metadata.ScriptFile!))
            .SelectMany(assemblyMetadata =>
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyMetadata.Key);

                return assemblyMetadata
                    .Select(metadata =>
                    {
                        var entryPointMatch = EntryPointRegex().Match(metadata.EntryPoint!);

                        return (
                            Metadata: metadata,
                            TypeName: entryPointMatch.Groups["typeName"].Value,
                            MethodName: entryPointMatch.Groups["methodName"].Value);
                    })
                    .GroupBy(context => context.TypeName)
                    .SelectMany(typeMetadata =>
                    {
                        var functionType = assembly.GetType(typeMetadata.Key)!;

                        return typeMetadata.Select(context =>
                        {
                            var methodInfo = functionType.GetMethod(context.MethodName)!;

                            var bindings = (context.Metadata.RawBindings ?? [])
                                .Select(bindingJson => JsonSerializer.Deserialize<FunctionBinding>(bindingJson, serializerOptions)!)
                                .ToArray();

                            var httpResultBinding = bindings.FirstOrDefault(binding =>
                                binding.Type == "http" &&
                                binding.Direction == BindingDirection.Out);

                            return new AspNetCoreFunctionMetadata
                            {
                                EntryPoint = context.Metadata.EntryPoint,
                                IsProxy = context.Metadata.IsProxy,
                                Language = context.Metadata.Language,
                                ManagedDependencyEnabled = context.Metadata.ManagedDependencyEnabled,
                                Name = context.Metadata.Name,
                                RawBindings = context.Metadata.RawBindings,
                                Bindings = bindings,
                                Retry = context.Metadata.Retry,
                                ScriptFile = context.Metadata.ScriptFile,
                                TargetMethod = methodInfo,
                                HttpResultBinding = httpResultBinding,
                                HttpResultDataType = httpResultBinding == null ? null : httpResultBinding switch
                                {
                                    { Name: "$return" } => TryUnwrapDataType(methodInfo.ReturnType),

                                    _ => TryUnwrapDataType(methodInfo.ReturnType)
                                        ?.GetProperty(httpResultBinding.Name)
                                        ?.PropertyType,
                                },
                            };
                        });
                    });
            })
            .DistinctBy(metadata => metadata.Name)
            .ToFrozenDictionary(metadata => metadata.Name!);
    }
}