namespace SimpleCallingBotEngine.Http;

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

public static class HttpExtensions
{
    public static HttpRequestMessage CreateRequestMessage(this HttpRequest request)
    {
        var displayUri = request.GetDisplayUrl();
        var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(displayUri),
            Method = new HttpMethod(request.Method),
        };

        if (request.ContentLength.HasValue && request.ContentLength.Value > 0)
        {
            httpRequest.Content = new StreamContent(request.Body);
        }

        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        return httpRequest;
    }
}
