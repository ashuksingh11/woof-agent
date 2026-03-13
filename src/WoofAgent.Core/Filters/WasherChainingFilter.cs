using Microsoft.SemanticKernel;

namespace WoofAgent.Core.Filters;

/// <summary>
/// Auto function invocation filter that chains check_detergent after schedule_washing.
/// This is the SK-native way to guarantee tool chaining — no LLM prompt dependency.
/// When schedule_washing completes, this filter programmatically invokes check_detergent
/// and appends its result, so the LLM sees both results together.
/// </summary>
public class WasherChainingFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        // Let the original function execute first
        await next(context);

        // After schedule_washing completes, automatically chain check_detergent
        if (context.Function.Name == "schedule_washing")
        {
            Console.WriteLine("[Filter] schedule_washing completed — chaining check_detergent...");

            var detergentResult = await context.Kernel.InvokeAsync(
                "WasherPlugin", "check_detergent");

            // Append detergent check result to the schedule_washing result
            var originalResult = context.Result.ToString();
            var combinedResult = $"""
                {originalResult}

                --- Detergent Status (auto-checked) ---
                {detergentResult}
                """;

            context.Result = new FunctionResult(context.Function, combinedResult);
        }
    }
}
