// Copyright (c) React Cosulting S.r.l., 2019. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for full license information.

namespace HelloCake
{
    public class Writer
    {
        /// <summary>
        /// Retrive greeting message.
        /// </summary>
        /// <param name="name">The name to be greeted.</param>
        /// <returns>Greeting message</returns>
        public string Greeting(string name) => string.IsNullOrWhiteSpace(name) ? $"Yo Bro!" : $"Hello {name}!";
    }
}
