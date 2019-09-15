using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace HelloCake
{
    public static class Program
    {
        public static void Main(string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .UseKestrel()
            .UseStartup<Startup>().Build().Run();
    }
}