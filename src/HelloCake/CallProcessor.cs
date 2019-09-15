using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HelloCake
{
    public static class HttpRequestExtensions
    {
        public static async Task<HttpResponse> ProcessAsync(
            this HttpRequest request,
            Func<HttpResponse> func)
        {
            
            return await Task.Run(() => func.Invoke());
        }
    }
}
