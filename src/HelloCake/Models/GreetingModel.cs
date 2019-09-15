using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HelloCake.Models
{
    public class GreetingModel
    {
        public string Name { get; private set; }

        public GreetingModel()
        {
        }

        public GreetingModel(string name) : this()
        {
            this.Name = name;
        }

        public async Task<GreetingModel> MapAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var content = await reader.ReadToEndAsync();
                var json = JsonConvert.DeserializeObject<dynamic>(content);
                this.Name = (string)json.name;
            }

            return this;
        }
    }
}
