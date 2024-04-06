﻿using System.Reflection;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal;

/// <summary>
/// Injects function types into the providers pipeline in order to create MVC application model
/// </summary>
/// <param name="functionMetadataProvider">AspNetCore Function Metadata Provider</param>
internal class FunctionApplicationModelProvider(
    AspNetCoreFunctionMetadataProvider functionMetadataProvider)
    : IApplicationModelProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public int Order => -10_000;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        var functionTypes = functionMetadataProvider.Metadata.Values
            .Select(metadata => metadata.TargetMethod.DeclaringType!.GetTypeInfo())
            .Distinct()
            .ToArray();

        // set functionTypes into the context and let other providers to run as usual
        typeof(ApplicationModelProviderContext)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(field => field.FieldType == typeof(IEnumerable<TypeInfo>))
            .SetValue(context, functionTypes);
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        this.RemoveHiddenActions(context);
        this.SetFunctionRoutingInfo(context);
    }

    private void SetFunctionRoutingInfo(ApplicationModelProviderContext context)
    {
        _ = context.Result.Controllers
            .SelectMany(controller => controller.Actions)
            .Join(
                functionMetadataProvider.Metadata.Values,
                action => action.ActionMethod,
                metadata => metadata.TargetMethod,
                (action, metadata) =>
                {
                    var httpTrigger = (metadata.RawBindings ?? Enumerable.Empty<string>())
                        .Select(bindingJson => JsonSerializer.Deserialize<FunctionHttpBinding>(bindingJson, SerializerOptions))
                        .FirstOrDefault();

                    if (httpTrigger?.Route != null && httpTrigger.Methods != null)
                    {
                        foreach (var selector in action.Selectors)
                        {
                            selector.AttributeRouteModel = new AttributeRouteModel { Template = $"api/{httpTrigger.Route}" };

                            if (httpTrigger.Methods.Length > 0)
                            {
                                var httpMethods = httpTrigger.Methods
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToArray();

                                selector.EndpointMetadata.Add(new HttpMethodMetadata(httpMethods));

                                selector.ActionConstraints.Add(new HttpMethodActionConstraint(httpMethods));
                            }
                        }
                    }

                    return action;
                })
            .ToArray();
    }

    private void RemoveHiddenActions(ApplicationModelProviderContext context)
    {
        foreach (var controller in context.Result.Controllers.ToList())
        {
            if (controller.ApiExplorer.IsVisible == false)
            {
                context.Result.Controllers.Remove(controller);

                continue;
            }

            foreach (var action in controller.Actions.Where(action => action.ApiExplorer.IsVisible == false).ToList())
            {
                controller.Actions.Remove(action);
            }
        }
    }
}

file sealed class FunctionHttpBinding
{
    public string? Route { get; set; }

    public string[]? Methods { get; set; }
}