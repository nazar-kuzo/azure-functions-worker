using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;

namespace AzureFunctions.Worker.Extensions.AspNetCore;

/// <summary>
/// Extends built-in <see cref="DefaultFunctionMetadata"/> with AspNetCore metadata.
/// </summary>
public sealed class AspNetCoreFunctionMetadata : DefaultFunctionMetadata
{
    /// <summary>
    /// Parsed metadata from <see cref="DefaultFunctionMetadata.RawBindings"/>
    /// </summary>
    public required FunctionBinding[] Bindings { get; set; }

    /// <summary>
    /// Function target <see cref="MethodInfo"/> that will be executed.
    /// </summary>
    public required MethodInfo TargetMethod { get; set; }

    /// <summary>
    /// Gets function HTTP result binding (if exists).
    /// </summary>
    public FunctionBinding? HttpResultBinding { get; set; }

    /// <summary>
    /// Gets function HTTP result type (if exists).
    /// If type is wrapped in <see cref="Task{TResult}"/> then it unwraps underlying type.
    /// </summary>
    public Type? HttpResultDataType { get; set; }

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

public sealed class FunctionBinding
{
    public required string Name { get; set; }

    public required BindingDirection Direction { get; set; }

    public required string Type { get; set; }

    public string? Route { get; set; }

    public string[]? Methods { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<BindingDirection>))]
public enum BindingDirection
{
    [JsonStringEnumMemberName("In")]
    In,

    [JsonStringEnumMemberName("Out")]
    Out,

    [JsonStringEnumMemberName("Inout")]
    InOut,
}