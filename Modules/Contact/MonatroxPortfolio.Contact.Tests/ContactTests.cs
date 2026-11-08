using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MonatroxPortfolio.Contact.Application;
using MonatroxPortfolio.Contact.Application.Abstractions;
using MonatroxPortfolio.Contact.Domain;
using MonatroxPortfolio.Contact.Public;
using MonatroxPortfolio.Shared.Contracts;

namespace MonatroxPortfolio.Contact.Tests;

public sealed class ContactTests
{
    private sealed class Sender : IContactSender
    {
        public List<ContactMessage> Messages { get; } = [];
        public bool Fail { get; set; }
        public ContactDeliveryInfoDto Info => new(!Fail, true);
        public Task<Result<ContactReceiptDto>> SendAsync(ContactMessage message, CancellationToken token)
        {
            if (Fail) return Task.FromResult(Result<ContactReceiptDto>.Failure("contact.unavailable", "Unavailable"));
            Messages.Add(message);
            return Task.FromResult(Result<ContactReceiptDto>.Success(new(true)));
        }
    }

    private static async Task<WebApplication> StartAsync(Sender sender)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IContactSender>(sender);
        builder.Services.AddScoped<ContactService>();
        builder.Services.AddContactRateLimiting();
        var app = builder.Build();
        app.UseRateLimiter();
        app.MapContactEndpoints();
        await app.StartAsync();
        return app;
    }
    private static HttpRequestMessage Request(string json, bool header = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/contact") { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
        if (header) request.Headers.Add("X-Portfolio-Contact", "1");
        return request;
    }
    private sealed class UnknownLengthContent : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(new byte[32769]).AsTask();
    }
    private const string Valid = "{\"name\":\"Visitor\",\"email\":\"visitor@example.com\",\"message\":\"Hello\"}";

    [Fact]
    public async Task Valid_request_has_fixed_recipient_and_explicit_capture_receipt()
    {
        var sender = new Sender();
        await using var app = await StartAsync(sender);
        using var client = app.GetTestClient();
        var info = await client.GetFromJsonAsync<ContactDeliveryInfoDto>("/api/contact");
        Assert.Equal(new(true, true), info);
        using var response = await client.SendAsync(Request(Valid));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<ContactReceiptDto>())!.CapturedLocally);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("zac@monatrox.com", ContactMessage.Recipient);
        Assert.Equal("visitor@example.com", Assert.Single(sender.Messages).Email);
    }

    [Theory]
    [InlineData("{\"name\":\"\",\"email\":\"a@example.com\",\"message\":\"Hello\"}")]
    [InlineData("{\"name\":\"Name\",\"email\":\"not-an-email\",\"message\":\"Hello\"}")]
    [InlineData("{\"name\":\"Name\\r\\nBcc: bad\",\"email\":\"a@example.com\",\"message\":\"Hello\"}")]
    [InlineData("{\"name\":\"Name\",\"email\":\"Display <a@example.com>\",\"message\":\"Hello\"}")]
    [InlineData("{\"name\":\"Name\",\"email\":\"a@example.com\",\"message\":\"\"}")]
    [InlineData("{\"name\":\"Name\",\"email\":\"a@example.com\",\"message\":\"Hello\",\"website\":\"bot.example\"}")]
    [InlineData("{\"name\":\"Name\",\"email\":\"a@example.com\",\"message\":\"Hello\",\"recipient\":\"other@example.com\"}")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Invalid_and_header_injection_requests_never_send(string json)
    {
        var sender = new Sender();
        await using var app = await StartAsync(sender);
        using var client = app.GetTestClient();
        using var response = await client.SendAsync(Request(json));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Browser_cross_site_guard_size_limit_and_failures_are_explicit()
    {
        var sender = new Sender { Fail = true };
        await using var app = await StartAsync(sender);
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(Request(Valid, false))).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.SendAsync(Request(new string('x', 32769)))).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.SendAsync(Request(Valid))).StatusCode);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Unknown_length_payloads_are_bounded_and_cross_origin_preflight_is_not_enabled()
    {
        var sender = new Sender();
        await using var app = await StartAsync(sender);
        using var client = app.GetTestClient();
        using var request = Request(Valid);
        request.Content = new UnknownLengthContent();
        request.Content.Headers.ContentType = new("application/json");
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.SendAsync(request)).StatusCode);
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/contact");
        preflight.Headers.Add("Origin", "https://other.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "X-Portfolio-Contact");
        using var response = await client.SendAsync(preflight);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Rate_limits_apply_even_to_rejected_submissions()
    {
        var sender = new Sender();
        await using var app = await StartAsync(sender);
        using var client = app.GetTestClient();
        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(Request("{}"))).StatusCode);
        var limited = await client.SendAsync(Request(Valid));
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public void Domain_bounds_text_and_preserves_plain_message()
    {
        var message = new ContactMessage(" Name ", "sender@example.com", "<script>literal</script>\n.line");
        Assert.Equal("Name", message.Name);
        Assert.Contains("<script>", message.Body);
        Assert.Throws<ArgumentException>(() => new ContactMessage(new string('a', 101), message.Email, message.Body));
        Assert.Throws<ArgumentException>(() => new ContactMessage(message.Name, message.Email, new string('a', 5001)));
        Assert.Throws<ArgumentException>(() => new ContactMessage(message.Name, message.Email, "bad\0body"));
    }
}
