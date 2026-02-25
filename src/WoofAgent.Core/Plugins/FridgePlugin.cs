using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace WoofAgent.Core.Plugins;

/// <summary>
/// Mock smart fridge plugin for Tizen demo.
/// All data is dummy/hardcoded for testing purposes.
/// </summary>
public class FridgePlugin
{
    [KernelFunction("get_calendar_events")]
    [Description("Gets upcoming calendar events for the family. Returns events for today and tomorrow.")]
    public string GetCalendarEvents()
    {
        return """
            📅 Today's Events:
            - 8:00 AM: Kids school drop-off
            - 10:00 AM: Team standup (Dad - remote)
            - 3:30 PM: Soccer practice pickup
            - 6:00 PM: Family dinner

            📅 Tomorrow's Events:
            - 9:00 AM: Dentist appointment (Mom)
            - 12:00 PM: Lunch with Grandma
            - 4:00 PM: Piano lesson (Emma)
            """;
    }

    [KernelFunction("get_fridge_inventory")]
    [Description("Gets the current inventory of items in the smart fridge, including quantities and expiration status.")]
    public string GetFridgeInventory()
    {
        return """
            🥬 Fresh Produce:
            - Milk (1 gallon) - expires in 3 days
            - Eggs (8 remaining) - expires in 5 days
            - Spinach - expires tomorrow ⚠️
            - Carrots (1 bag) - fresh
            - Apples (4) - fresh

            🥩 Proteins:
            - Chicken breast (2 lbs) - frozen
            - Ground beef (1 lb) - expires in 2 days
            - Salmon fillet - expires tomorrow ⚠️

            🧀 Dairy:
            - Cheddar cheese - fresh
            - Yogurt (3 cups) - expires in 4 days
            - Butter - fresh

            🥤 Beverages:
            - Orange juice (half full)
            - Sparkling water (6 cans)

            ⚠️ Low stock: Milk, Eggs
            """;
    }

    [KernelFunction("suggest_recipes")]
    [Description("Suggests recipes based on available fridge inventory and optionally dietary preferences.")]
    public string SuggestRecipes(
        [Description("Optional dietary preference like 'vegetarian', 'low-carb', 'quick meals'")] string? preference = null)
    {
        var baseRecipes = """
            Based on your fridge inventory, here are some recipe suggestions:

            🍳 Quick & Easy (under 30 min):
            1. Spinach & Cheese Omelette - Use the spinach before it expires!
               Ingredients: eggs, spinach, cheddar cheese, butter

            2. Chicken Stir-fry with Carrots
               Ingredients: chicken breast, carrots, soy sauce (pantry)

            🍽️ Family Dinners:
            3. Baked Salmon with Roasted Vegetables - Use salmon today!
               Ingredients: salmon fillet, carrots, olive oil (pantry)

            4. Beef Tacos
               Ingredients: ground beef, cheddar cheese, tortillas (pantry)

            💡 Tip: The spinach and salmon expire tomorrow - consider using them today!
            """;

        if (!string.IsNullOrEmpty(preference))
        {
            return $"Filtering for: {preference}\n\n{baseRecipes}";
        }

        return baseRecipes;
    }

    [KernelFunction("get_weather")]
    [Description("Gets the current weather and forecast for the local area.")]
    public string GetWeather()
    {
        return """
            🌤️ Current Weather: Seoul, South Korea
            Temperature: 12°C (54°F)
            Condition: Partly Cloudy
            Humidity: 65%

            📅 Today's Forecast:
            - Morning: 10°C, Cloudy
            - Afternoon: 15°C, Partly Sunny
            - Evening: 11°C, Clear

            📅 Tomorrow:
            - High: 17°C | Low: 8°C
            - Condition: Sunny

            👕 Recommendation: Light jacket for morning, comfortable by afternoon.
            """;
    }

    [KernelFunction("set_timer")]
    [Description("Sets a kitchen timer with a name and duration.")]
    public string SetTimer(
        [Description("Name or label for the timer (e.g., 'pasta', 'oven', 'eggs')")] string timerName,
        [Description("Duration in minutes")] int minutes)
    {
        return $"""
            ⏱️ Timer Set!
            Name: {timerName}
            Duration: {minutes} minutes

            I'll alert you when the timer is done.
            Say "check timers" to see active timers.
            """;
    }

    [KernelFunction("add_to_shopping_list")]
    [Description("Adds an item to the family shopping list with optional quantity.")]
    public string AddToShoppingList(
        [Description("Item to add to the shopping list")] string item,
        [Description("Quantity or amount (e.g., '2', '1 gallon', '500g')")] string? quantity = null)
    {
        var quantityStr = string.IsNullOrEmpty(quantity) ? "" : $" ({quantity})";

        return $"""
            ✅ Added to Shopping List:
            • {item}{quantityStr}

            📝 Current Shopping List:
            • Milk (1 gallon)
            • Bread
            • Eggs (1 dozen)
            • {item}{quantityStr} ← New

            💡 Tip: Say "show shopping list" to see all items.
            """;
    }

    [KernelFunction("get_fridge_status")]
    [Description("Gets the overall status of the smart fridge including temperature and alerts.")]
    public string GetFridgeStatus()
    {
        return """
            🧊 Smart Fridge Status

            Temperature:
            - Fridge: 3°C (optimal: 1-4°C) ✅
            - Freezer: -18°C (optimal: -18°C) ✅

            Door Status: Closed ✅

            ⚠️ Alerts:
            - 2 items expiring soon (spinach, salmon)
            - Milk running low (reorder suggested)

            Energy Mode: Eco Mode Active
            Last Filter Change: 45 days ago (change in 45 days)
            """;
    }
}
