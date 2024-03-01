using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace AzureFunctions.Worker.Extensions.AspNetCore;

/// <summary>
/// Wraps built-in <see cref="IFunctionMetadataProvider"/> provider to return custom <see cref="AspNetCoreFunctionMetadata"/>.
/// </summary>
/// <param name="functionMetadataProvider">Built-in <see cref="IFunctionMetadataProvider"/></param>
/// <param name="modelBinderFactory">Model binder factory</param>
/// <param name="modelMetadataProvider">Model metadata provider</param>
/// <param name="applicationModelProviders">Application model providers</param>
public sealed partial class AspNetCoreFunctionMetadataProvider(
    IFunctionMetadataProvider functionMetadataProvider,
    IModelBinderFactory modelBinderFactory,
    IModelMetadataProvider modelMetadataProvider,
    IEnumerable<IApplicationModelProvider> applicationModelProviders)
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
            return TryUnwrapDataType(type.GetGenericArguments()[0]);
        }

        return type;
    }

    [GeneratedRegex("^(?<typeName>.*)\\.(?<methodName>\\S*)$")]
    private static partial Regex EntryPointRegex();

    private FrozenDictionary<string, AspNetCoreFunctionMetadata> InitializeMetadata()
    {
        var scriptRoot = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY")!;

        var functionsMetadata = functionMetadataProvider
            .GetFunctionMetadataAsync(scriptRoot)
            .GetAwaiter()
            .GetResult()
            .GroupBy(metadata => Path.Combine(scriptRoot, metadata.ScriptFile!))
            .SelectMany(assemblyMetadata =>
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyMetadata.Key);

                return assemblyMetadata.Select(metadata =>
                {
                    var entryPointMatch = EntryPointRegex().Match(metadata.EntryPoint!);
                    var functionType = assembly.GetType(entryPointMatch.Groups["typeName"].Value)!;
                    var methodInfo = functionType.GetMethod(entryPointMatch.Groups["methodName"].Value)!;

                    return new AspNetCoreFunctionMetadata
                    {
                        EntryPoint = metadata.EntryPoint,
                        IsProxy = metadata.IsProxy,
                        Language = metadata.Language,
                        ManagedDependencyEnabled = metadata.ManagedDependencyEnabled,
                        Name = metadata.Name,
                        RawBindings = metadata.RawBindings,
                        Retry = metadata.Retry,
                        ScriptFile = metadata.ScriptFile,
                        TargetMethod = methodInfo,
                        ReturnDataType = TryUnwrapDataType(methodInfo.ReturnType),
                        CustomAttributes = methodInfo
                            .GetCustomAttributes(true)
                            .Union(functionType.GetCustomAttributes(true))
                            .ToImmutableArray(),
                    };
                });
            })
            .DistinctBy(metadata => metadata.Name)
            .ToList();

        var functionTypes = functionsMetadata
            .Select(metadata => metadata.TargetMethod.DeclaringType!)
            .Distinct();

        _ = functionsMetadata.Join(
                this.GetFunctionDescriptors(functionTypes),
                metadata => metadata.TargetMethod,
                actionDescriptor => actionDescriptor.MethodInfo,
                (metadata, actionDescriptor) =>
                {
                    actionDescriptor.DisplayName = metadata.Name;

                    metadata.ActionDescriptor = actionDescriptor;

                    metadata.AspNetCoreParameters = actionDescriptor.Parameters
                        .OfType<ControllerParameterDescriptor>()
                        .Where(parameter =>
                            parameter.BindingInfo?.BindingSource != null &&
                            parameter.BindingInfo.BindingSource != BindingSource.Special)
                        .Select(parameter =>
                        {
                            var modelMetadata = ((ModelMetadataProvider) modelMetadataProvider)
                                .GetMetadataForParameter(parameter.ParameterInfo);

                            var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
                            {
                                BindingInfo = parameter.BindingInfo,
                                Metadata = modelMetadata,
                                CacheToken = parameter,
                            });

                            return new AspNetCoreParameterBindingInfo(binder, modelMetadata, parameter);
                        })
                        .Where(item => item.ModelMetadata.IsBindingAllowed)
                        .ToArray();

                    return metadata;
                })
            .ToArray();

        return functionsMetadata.ToFrozenDictionary(metadata => metadata.Name!);
    }

    private IList<ControllerActionDescriptor> GetFunctionDescriptors(IEnumerable<Type> functionTypes)
    {
        var context = new ApplicationModelProviderContext(functionTypes.Select(type => type.GetTypeInfo()));

        var orderedProviders = applicationModelProviders.OrderBy(p => p.Order);

        foreach (var provider in orderedProviders)
        {
            provider.OnProvidersExecuting(context);
        }

        foreach (var provider in orderedProviders)
        {
            provider.OnProvidersExecuted(context);
        }

        return (IList<ControllerActionDescriptor>) typeof(ApplicationModel).Assembly
            .GetType("Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerActionDescriptorBuilder")!
            .GetMethod("Build", BindingFlags.Static | BindingFlags.Public)!
            .Invoke(null, new[] { context.Result })!;
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
    /// Collection of custom attributes provided on <see cref="TargetMethod"/> level and it's declaring type level.
    /// </summary>
    public required ImmutableArray<object> CustomAttributes { get; set; }

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
