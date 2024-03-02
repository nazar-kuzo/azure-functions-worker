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