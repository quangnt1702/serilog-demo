using System;
using System.IO;
using System.Text;
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
                
                await _next(context);

                var response = await FormatResponse(context.Response);

                Log.Information("[Request Response Logger]" + request + response);
                await responseBody.CopyToAsync(originalBodyStream);
            }


        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableBuffering();
            MemoryStream requestBody = new MemoryStream();
            await request.Body.CopyToAsync(requestBody);
            requestBody.Seek(0, SeekOrigin.Begin);
            string bodyAsText = await new StreamReader(requestBody).ReadToEndAsync();
            
            //We need to reset the reader for the response so that the client can read it.
            request.Body.Position = 0;
            
            return "\n" + "===== BEGIN REQUEST =====" + "\n" +
                   $"Origin: {request.Headers["Origin"]}" + "\n" +
                   $"Method: {request.Method}" + "\n" +
                   $"Scheme: {request.Scheme}" + "\n" +
                   $"Host: {request.Host}" + "\n" +
                   $"Path: {request.Path}" + "\n" +
                   $"QueryString: {request.QueryString}" + "\n" +
                   $"Request Body: {bodyAsText}";
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