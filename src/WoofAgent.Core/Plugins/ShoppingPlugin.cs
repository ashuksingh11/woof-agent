using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace WoofAgent.Core.Plugins;

/// <summary>
/// Mock shopping plugin for adding items to a shopping cart.
/// </summary>
public class ShoppingPlugin
{
    [KernelFunction("add_to_cart")]
    [Description("Adds an item to the user's online shopping cart for purchase.")]
    public string AddToCart(
        [Description("The item to add to the shopping cart (e.g., 'liquid detergent', 'fabric softener')")] string item,
        [Description("Quantity to add (default 1)")] int quantity = 1)
    {
        return $"""
            Item added to shopping cart.
            Item: {item}
            Quantity: {quantity}
            Estimated price: $12.99
            Cart total: 1 item(s)
            Checkout ready: Yes
            """;
    }
}
