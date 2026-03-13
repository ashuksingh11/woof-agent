using Microsoft.SemanticKernel;

namespace WoofAgent.Core.Filters;

/// <summary>
/// Auto function invocation filter that chains Swiggy MCP tools.
///
/// Chaining rules:
/// 1. search_restaurants → auto-calls get_restaurant_menu for the top result
/// 2. add_item_to_cart  → auto-calls get_cart_summary + get_delivery_eta
/// 3. place_order       → auto-calls get_order_status
///
/// Tool names are configurable — update them to match your MCP server's actual tool names.
/// The plugin name is also configurable (defaults to "SwiggyFood").
/// </summary>
public class SwiggyChainingFilter : IAutoFunctionInvocationFilter
{
    private readonly string _pluginName;
    private readonly SwiggyChainingConfig _config;

    public SwiggyChainingFilter(string pluginName, SwiggyChainingConfig? config = null)
    {
        _pluginName = pluginName;
        _config = config ?? new SwiggyChainingConfig();
    }

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        // Let the original function execute first
        await next(context);

        // Only chain tools from the configured Swiggy plugin
        if (context.Function.PluginName != _pluginName)
            return;

        var functionName = context.Function.Name;
        var originalResult = context.Result.ToString();

        // Chain 1: search_restaurants → get_restaurant_menu
        if (functionName == _config.SearchRestaurants)
        {
            Console.WriteLine($"[Chain] {_config.SearchRestaurants} → {_config.GetRestaurantMenu}");

            var menuResult = await InvokeIfExistsAsync(
                context.Kernel, _config.GetRestaurantMenu,
                new() { ["restaurant_id"] = "top_result" });

            if (menuResult != null)
            {
                context.Result = new FunctionResult(context.Function, $"""
                    {originalResult}

                    --- Menu (auto-fetched for top result) ---
                    {menuResult}
                    """);
            }
        }
        // Chain 2: add_item_to_cart → get_cart_summary + get_delivery_eta
        else if (functionName == _config.AddItemToCart)
        {
            Console.WriteLine($"[Chain] {_config.AddItemToCart} → {_config.GetCartSummary} + {_config.GetDeliveryEta}");

            var cartTask = InvokeIfExistsAsync(context.Kernel, _config.GetCartSummary);
            var etaTask = InvokeIfExistsAsync(context.Kernel, _config.GetDeliveryEta);
            await Task.WhenAll(cartTask, etaTask);

            var combined = originalResult;
            if (cartTask.Result != null)
                combined += $"\n\n--- Cart Summary (auto-fetched) ---\n{cartTask.Result}";
            if (etaTask.Result != null)
                combined += $"\n\n--- Delivery ETA (auto-fetched) ---\n{etaTask.Result}";

            context.Result = new FunctionResult(context.Function, combined);
        }
        // Chain 3: place_order → get_order_status
        else if (functionName == _config.PlaceOrder)
        {
            Console.WriteLine($"[Chain] {_config.PlaceOrder} → {_config.GetOrderStatus}");

            var statusResult = await InvokeIfExistsAsync(
                context.Kernel, _config.GetOrderStatus);

            if (statusResult != null)
            {
                context.Result = new FunctionResult(context.Function, $"""
                    {originalResult}

                    --- Order Status (auto-fetched) ---
                    {statusResult}
                    """);
            }
        }
    }

    /// <summary>
    /// Safely invokes a kernel function — returns null if the function doesn't exist on the MCP server.
    /// This is important because MCP tools are discovered at runtime and may vary.
    /// </summary>
    private async Task<FunctionResult?> InvokeIfExistsAsync(
        Kernel kernel, string functionName, KernelArguments? args = null)
    {
        try
        {
            return await kernel.InvokeAsync(_pluginName, functionName, args ?? new());
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine($"[Chain] Tool '{functionName}' not found on MCP server — skipping");
            return null;
        }
    }
}

/// <summary>
/// Configuration for Swiggy tool names. Update these to match your MCP server's actual tool names.
/// Defaults are based on common Swiggy MCP server conventions.
/// </summary>
public class SwiggyChainingConfig
{
    // Chain 1: search → menu
    public string SearchRestaurants { get; set; } = "search_restaurants";
    public string GetRestaurantMenu { get; set; } = "get_restaurant_menu";

    // Chain 2: add to cart → cart summary + ETA
    public string AddItemToCart { get; set; } = "add_item_to_cart";
    public string GetCartSummary { get; set; } = "get_cart_summary";
    public string GetDeliveryEta { get; set; } = "get_delivery_eta";

    // Chain 3: place order → order status
    public string PlaceOrder { get; set; } = "place_order";
    public string GetOrderStatus { get; set; } = "get_order_status";
}
