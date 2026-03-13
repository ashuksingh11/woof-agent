using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace WoofAgent.Core.Plugins;

/// <summary>
/// Mock smart washer plugin for testing tool chaining.
/// schedule_washing -> check_detergent -> (agent suggests add_to_cart)
/// </summary>
public class WasherPlugin
{
    [KernelFunction("schedule_washing")]
    [Description("Schedules a washing cycle at the specified time. Returns confirmation with the scheduled time.")]
    public string ScheduleWashing(
        [Description("The time to schedule the washing (e.g., '10 PM', '22:00', 'tomorrow 8 AM')")] string time)
    {
        return $"""
            Washing cycle scheduled successfully.
            Scheduled time: {time}
            Mode: Normal wash (40°C)
            Estimated duration: 55 minutes
            Status: Queued
            """;
    }

    [KernelFunction("check_detergent")]
    [Description("Checks the current detergent level in the smart washer. Returns the detergent level and whether it needs refilling.")]
    public string CheckDetergent()
    {
        return """
            Detergent level: LOW (15% remaining)
            Estimated washes remaining: 2
            Detergent type: Liquid (Standard)
            Recommendation: Refill soon to avoid interruption
            """;
    }
}
