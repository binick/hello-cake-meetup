// Copyright (c) React Cosulting S.r.l., 2019. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for full license information.

using Xunit;

namespace HelloCake.Spec.Specs
{
    public class HelloCakeSpec
    {
        private readonly Writer _writer;

        public HelloCakeSpec() => this._writer = new Writer();

        [Fact(DisplayName = "given a name when print greeting message then print name")]
        [Trait("Category", "Spec")]
        public void ScenarioWithKnowName()
        {
            // Arrange
            string name = "Peter";

            // Act
            string greeting = this._writer.Greeting(name);

            // Assert
            Assert.Equal($"Hello {name}!", greeting);
        }

        [Theory(DisplayName = "given an empty name when print greeting message then print name")]
        [Trait("Category", "Spec")]
        [InlineData("")]
        [InlineData(null)]
        public void ScenarioWithUnknown(string name)
        {
            // Act
            string greeting = this._writer.Greeting(name);

            // Assert
            Assert.Equal($"Yo Bro!", greeting);
        }
    }
}
