using HelloCake.Endpoints;
using HelloCake.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelloCake
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouter(router =>
            {
                router.MapGet("api/greeting/{name?}",
                    async (request, response, routeData) => await response
                        .WriteAsync(new WriterEndpoint()
                            .Greeting(new GreetingModel((string)request.HttpContext.GetRouteValue("name")))));

                router.MapPost("api/greeting",
                     async (request, response, routeData) => await response
                         .WriteAsync(new WriterEndpoint()
                             .Greeting(await new GreetingModel().MapAsync(request.Body))));
            });
        }
    }
}