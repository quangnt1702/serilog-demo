using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace SerilogDemo.Middlewares
{
    public class RequestResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string request = await FormatRequest(context.Request);

            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;
                
                await _next.Invoke(context);

                var response = await FormatResponse(context.Response);

                Log.Information("[Request Response Logger]" + request + response);
                await responseBody.CopyToAsync(originalBodyStream);
            }


        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            MemoryStream requestBody = new MemoryStream();

            await request.Body.CopyToAsync(requestBody);

            requestBody.Seek(0, SeekOrigin.Begin);
            string requestBodyContent = await new StreamReader(requestBody).ReadToEndAsync();
            requestBody.Seek(0, SeekOrigin.Begin);

            return "\n" + "===== BEGIN REQUEST =====" + "\n" +
                   $"Origin: {request.Headers["Origin"]}" + "\n" +
                   $"Method: {request.Method}" + "\n" +
                   $"Scheme: {request.Scheme}" + "\n" +
                   $"Host: {request.Host}" + "\n" +
                   $"Path: {request.Path}" + "\n" +
                   $"QueryString: {request.QueryString}" + "\n" +
                   $"Request Body: {requestBodyContent}";
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string responseBodyContent = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);


            return "\n" + "===== BEGIN RESPONSE =====" + "\n" +
                   $"Status: {response.StatusCode}" + "\n" +
                   $"Response Body: {responseBodyContent}";
        }
    }
}