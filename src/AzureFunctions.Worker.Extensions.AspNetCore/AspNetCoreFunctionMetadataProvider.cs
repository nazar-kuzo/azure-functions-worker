using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    private static Type TryUnwrapDataType(Type type)
    {
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() is { } genericTypeDefinition &&
            genericTypeDefinition == typeof(Task<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    [GeneratedRegex("^(?<typeName>.*)\\.(?<methodName>\\S*)$")]
    private static partial Regex EntryPointRegex();

    private FrozenDictionary<string, AspNetCoreFunctionMetadata> InitializeMetadata()
    {
        var scriptRoot = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY")!;

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

                            return new AspNetCoreFunctionMetadata
                            {
                                EntryPoint = context.Metadata.EntryPoint,
                                IsProxy = context.Metadata.IsProxy,
                                Language = context.Metadata.Language,
                                ManagedDependencyEnabled = context.Metadata.ManagedDependencyEnabled,
                                Name = context.Metadata.Name,
                                RawBindings = context.Metadata.RawBindings,
                                Retry = context.Metadata.Retry,
                                ScriptFile = context.Metadata.ScriptFile,
                                TargetMethod = methodInfo,
                                ReturnDataType = TryUnwrapDataType(methodInfo.ReturnType),
                            };
                        });
                    });
            })
            .DistinctBy(metadata => metadata.Name)
            .ToFrozenDictionary(metadata => metadata.Name!);
    }
}

/// <summary>
/// Extends built-in <see cref="DefaultFunctionMetadata"/> with AspNetCore metadata.
/// </summary>
public sealed class AspNetCoreFunctionMetadata : DefaultFunctionMetadata
{
    /// <summary>
    /// Function target <see cref="MethodInfo"/> that will be executed.
    /// </summary>
    public required MethodInfo TargetMethod { get; set; }

    /// <summary>
    /// Gets function result type. If type is wrapped in <see cref="Task{TResult}"/> then it unwraps underlying type.
    /// </summary>
    public required Type ReturnDataType { get; set; }

    /// <summary>
    /// AspNetCore action descriptor that contains information about parameter binding or api explorer descriptions
    /// </summary>
    public ControllerActionDescriptor ActionDescriptor { get; set; } = default!;

    /// <summary>
    /// Collection of AspNetCore function parameters and their binding metadata
    /// </summary>
    public AspNetCoreParameterBindingInfo[] AspNetCoreParameters { get; set; } = [];
}

/// <summary>
/// Provides information about AspNetCore function parameters and their binding metadata
/// </summary>
/// <param name="ModelBinder">Model binder</param>
/// <param name="ModelMetadata">Model metadata</param>
/// <param name="Parameter">Parameter descriptor</param>
public sealed record AspNetCoreParameterBindingInfo(
    IModelBinder ModelBinder,
    ModelMetadata ModelMetadata,
    ParameterDescriptor Parameter)
{
}
