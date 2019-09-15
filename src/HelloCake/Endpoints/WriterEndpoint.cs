using HelloCake.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace HelloCake.Endpoints
{
    public class WriterEndpoint
    {
        public string Greeting(GreetingModel model) => new Writer().Greeting(model.Name);
    }
}
