using Xunit;

namespace CheckAppBackend.Tests;

public class SimulatedFailureTests
{
    // Toggle flag for CI/CD quality gate error simulation
    public const bool SimulationFixed = false;

    [Fact]
    public void TC_SIM_BE_001_Verify_Order_Cancellation_Restock_Policy()
    {
        var initialStock = 45;
        var orderQuantity = 2;
        var isCancelled = true;

        // When order is cancelled, inventory must be restored to initialStock
        var finalStock = isCancelled ? initialStock : initialStock - orderQuantity;

        if (!SimulationFixed)
        {
            // INTENTIONAL FAILURE FOR CI/CD SIMULATION:
            // Asserts stock was depleted to 0 upon cancellation instead of restored to 45
            Assert.Equal(0, finalStock);
        }
        else
        {
            // THE FIX:
            // Correct inventory restoration assertion
            Assert.Equal(45, finalStock);
        }
    }
}

