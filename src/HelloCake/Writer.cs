namespace HelloCake
{
    public class Writer
    {
        /// <summary>
        /// Retrive greeting message.
        /// </summary>
        /// <param name="name">The name to be greeted.</param>
        /// <returns>Greeting message</returns>
        public string Greeting(string name) => string.IsNullOrWhiteSpace(name) ? $"Hello Cake!" : $"Hello {name}!";
    }
}
