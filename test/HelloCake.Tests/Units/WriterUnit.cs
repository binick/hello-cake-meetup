using Xunit;

namespace HelloCake.Tests.Unit
{
    public class WriterUnit
    {
        private readonly Writer _writer;

        public WriterUnit() => this._writer = new Writer();

        [Fact(DisplayName = "given a name when print greeting message then print name")]
        public void ScenarioWithKnowName()
        {
            // Given
            string name = "Cake";

            // When
            string greeting = this._writer.Greeting(name);

            // Then
            Assert.Equal($"Hello {name}!", greeting);
        }

        [Theory(DisplayName = "given an empty name when print greeting message then print name")]
        [InlineData("")]
        [InlineData(null)]
        public void ScenarioWithUnknown(string p0)
        {
            // Given
            var name = p0;

            // When
            string greeting = this._writer.Greeting(name);

            // Then
            Assert.Equal($"Build with Cake!", greeting);
        }
    }
}
