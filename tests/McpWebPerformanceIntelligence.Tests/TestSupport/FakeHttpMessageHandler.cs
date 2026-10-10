using System.Net;

namespace McpWebPerformanceIntelligence.Tests.TestSupport;

/// <summary>
/// Deterministic <see cref="HttpMessageHandler"/> stand-in for the PageSpeed API so
/// <see cref="Services.PerformanceAnalyzer"/> can be tested without real network calls.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public static FakeHttpMessageHandler ReturningJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
    }

    public static FakeHttpMessageHandler ReturningStatus(HttpStatusCode statusCode)
    {
        return new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responder(request));
    }
}
