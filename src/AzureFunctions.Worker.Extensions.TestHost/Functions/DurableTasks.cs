using AzureFunctions.Worker.Extensions.TestHost.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace AzureFunctions.Worker.Extensions.TestHost.Functions;

public sealed class DurableTasks
{
    [Function(nameof(DurableTasks) + "_" + nameof(Orchestration))]
    public async Task Orchestration(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        UserInfo user)
    {
        await context.CallSubOrchestratorAsync(() =>
            this.ExplicitInputUserInfoOrchestration(OrchestrationContext.Default, user));

        await context.CallSubOrchestratorAsync(() =>
            this.ImplicitInputUserInfoOrchestration(OrchestrationContext.Create(user)));
    }

    [Function(nameof(DurableTasks) + "_" + nameof(ExplicitInputUserInfoOrchestration))]
    public async Task ExplicitInputUserInfoOrchestration(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        UserInfo user)
    {
        await context.CallActivityAsync(() => this.UserInfoActivity(user));
        await context.CallActivityAsync(() => this.EmptyActivity(ActivityContext.Default));
        await context.CallActivityAsync(() => this.MultipleParamsActivity(ValueTuple.Create(user, user.Id)));
    }

    [Function(nameof(DurableTasks) + "_" + nameof(ImplicitInputUserInfoOrchestration))]
    public async Task ImplicitInputUserInfoOrchestration(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var user = context.GetInput<UserInfo>()!;

        await context.CallActivityAsync(() => this.UserInfoActivity(user));
        await context.CallActivityAsync(() => this.EmptyActivity(ActivityContext.Default));
        await context.CallActivityAsync(() => this.MultipleParamsActivity(ValueTuple.Create(user, user.Id)));
    }

    [Function(nameof(DurableTasks) + "_" + nameof(UserInfoActivity))]
    public Task UserInfoActivity([ActivityTrigger] UserInfo user)
    {
        return Task.CompletedTask;
    }

    [Function(nameof(DurableTasks) + "_" + nameof(EmptyActivity))]
    public Task EmptyActivity([ActivityTrigger] TaskActivityContext context)
    {
        return Task.CompletedTask;
    }

    [Function(nameof(DurableTasks) + "_" + nameof(MultipleParamsActivity))]
    public Task MultipleParamsActivity([ActivityTrigger] (UserInfo User, Guid Id) context)
    {
        return Task.CompletedTask;
    }
}
