using Xunit;
using static Program;

namespace Scorp.CoddingChallenge
{
    public class AccountBalanceTests
    {
        [Fact]
        public void DecreaseAmount_ShouldDeductAndReturnTrue_WhenSufficientFunds()
        {
            // Arrange
            var balance = new AccountBalance("USD", 100);

            // Act
            var result = balance.DecreaseAmount(30);

            // Assert
            Assert.True(result);
            Assert.Equal(70, balance.CurrentAmount);
        }

        [Fact]
        public void DecreaseAmount_ShouldReturnFalseAndPreserveBalance_WhenInsufficientFunds()
        {
            // Arrange
            var balance = new AccountBalance("USD", 50);

            // Act
            var result = balance.DecreaseAmount(100);

            // Assert
            Assert.False(result);
            Assert.Equal(50, balance.CurrentAmount);  // Önemli: state değişmedi
        }

        [Theory]  // ← Parametreli test
        [InlineData(100, 100, true, 0)]    // Tam tüketim
        [InlineData(100, 0, true, 100)]    // Sıfır çekim (edge case)
        [InlineData(100, 101, false, 100)] // 1 fazla
        public void DecreaseAmount_VariousScenarios(
            int initial, int requested, bool expectedResult, int expectedRemaining)
        {
            var balance = new AccountBalance("USD", initial);
            var result = balance.DecreaseAmount(requested);

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedRemaining, balance.CurrentAmount);
        }
    }
}
