using System.Net;
using System.Text;
using System.Web;

namespace ConvictionCore.Tests.Shared;

public sealed record FlexCall(string Endpoint, Uri Uri, string? UserAgent, DateTimeOffset At)
{
    public string? Query(string name) => HttpUtility.ParseQueryString(Uri.Query)[name];
}

/// <summary>
/// Scripted stand-in for the Flex Web Service. Queued responses are used first; then the
/// per-endpoint default. No real IBKR call is ever made.
/// </summary>
public sealed class FakeFlexHandler(TimeProvider time) : HttpMessageHandler
{
    private readonly Queue<Func<FlexCall, HttpResponseMessage>> _sendRequest = new();
    private readonly Queue<Func<FlexCall, HttpResponseMessage>> _getStatement = new();
    private readonly List<FlexCall> _calls = [];

    public Func<FlexCall, HttpResponseMessage>? DefaultSendRequest { get; set; } = _ => Xml(FlexFixtures.ReadText("flex-responses/send-request-success.xml"));

    public Func<FlexCall, HttpResponseMessage>? DefaultGetStatement { get; set; }

    public IReadOnlyList<FlexCall> Calls
    {
        get
        {
            lock (_calls)
            {
                return [.. _calls];
            }
        }
    }

    public IEnumerable<FlexCall> CallsTo(string endpoint) => Calls.Where(call => call.Endpoint == endpoint);

    public FakeFlexHandler ServeStatement(byte[] payload)
    {
        DefaultGetStatement = _ => Bytes(payload);
        return this;
    }

    public FakeFlexHandler EnqueueSendRequest(Func<FlexCall, HttpResponseMessage> response)
    {
        _sendRequest.Enqueue(response);
        return this;
    }

    public FakeFlexHandler EnqueueGetStatement(Func<FlexCall, HttpResponseMessage> response)
    {
        _getStatement.Enqueue(response);
        return this;
    }

    public static HttpResponseMessage Xml(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "text/xml") };

    public static HttpResponseMessage Bytes(byte[] body) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };

    public static Func<FlexCall, HttpResponseMessage> Error(string code) =>
        _ => Xml(FlexFixtures.ReadText($"flex-responses/error-{code}.xml"));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("Request without URI.");
        var call = new FlexCall(
            uri.Segments[^1],
            uri,
            request.Headers.UserAgent.Count == 0 ? null : request.Headers.UserAgent.ToString(),
            time.GetUtcNow());
        lock (_calls)
        {
            _calls.Add(call);
        }

        var (queue, fallback) = call.Endpoint switch
        {
            "SendRequest" => (_sendRequest, DefaultSendRequest),
            "GetStatement" => (_getStatement, DefaultGetStatement),
            _ => throw new InvalidOperationException($"Unexpected Flex endpoint {call.Endpoint}.")
        };

        Func<FlexCall, HttpResponseMessage>? responder;
        lock (queue)
        {
            responder = queue.Count > 0 ? queue.Dequeue() : fallback;
        }

        return Task.FromResult((responder ?? throw new InvalidOperationException($"No scripted response for {call.Endpoint}."))(call));
    }
}
