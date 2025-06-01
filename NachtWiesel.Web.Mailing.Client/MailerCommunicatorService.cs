using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NachtWiesel.Web.Mailing.Client;

public interface IMailerCommunicatorService
{
    Task SendEmailAsync(string userName, string email, string subject, string message, DateTimeOffset? offset = null);
    Task SendEmailAsync<TComponent>(string userName, string email, string subject, DateTimeOffset? offset = null) where TComponent : IComponent;
    Task SendEmailAsync<TComponent>(string userName, string email, string subject, ParameterView view, DateTimeOffset? offset = null) where TComponent : IComponent;
    Task SendEmailAsync<TComponent>(string userName, string email, string subject, Dictionary<string, object?>? dictionary = null, DateTimeOffset? offset = null) where TComponent : IComponent;

    Task SendEmailAsync(IEnumerable<MailingRequestRecepient> recepients, string subject, string message, DateTimeOffset? offset = null);
    Task SendEmailAsync<TComponent>(IEnumerable<MailingRequestRecepient> recepients, string subject, DateTimeOffset? offset = null) where TComponent : IComponent;
    Task SendEmailAsync<TComponent>(IEnumerable<MailingRequestRecepient> recepients, string subject, ParameterView view, DateTimeOffset? offset = null) where TComponent : IComponent;
    Task SendEmailAsync<TComponent>(IEnumerable<MailingRequestRecepient> recepients, string subject, Dictionary<string, object?>? dictionary = null, DateTimeOffset? offset = null) where TComponent : IComponent;
}

public sealed class MailerCommunicatorService : IMailerCommunicatorService
{
    private readonly ILogger _logger;
    private readonly HtmlRenderer _renderer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MailerCommunicatorServiceConfig _config;
    public MailerCommunicatorService(ILoggerFactory loggerFactory,
                                     IServiceProvider serviceProvider,
                                     IHttpClientFactory httpClientFactory,
                                     MailerCommunicatorServiceConfig config)
    {
        _logger = loggerFactory.CreateLogger<MailerCommunicatorService>();
        _renderer = new HtmlRenderer(serviceProvider, loggerFactory);
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public Task SendEmailAsync(string userName, string email, string subject, string message, DateTimeOffset? offset = null)
        => SendEmailAsync([new MailingRequestRecepient(userName, email)], subject, message, offset);

    public Task SendEmailAsync<TComponent>(string userName, string email, string subject, DateTimeOffset? offset = null) where TComponent : IComponent
        => SendEmailAsync<TComponent>([new MailingRequestRecepient(userName, email)], subject, offset);

    public Task SendEmailAsync<TComponent>(string userName, string email, string subject, ParameterView view, DateTimeOffset? offset = null) where TComponent : IComponent
        => SendEmailAsync<TComponent>([new MailingRequestRecepient(userName, email)], subject, view, offset);

    public Task SendEmailAsync<TComponent>(string userName, string email, string subject, Dictionary<string, object?>? dictionary = null, DateTimeOffset? offset = null) where TComponent : IComponent
        => SendEmailAsync<TComponent>([new MailingRequestRecepient(userName, email)], subject, dictionary, offset);

    public async Task SendEmailAsync(IEnumerable<MailingRequestRecepient> recepients, string subject, string message, DateTimeOffset? offset = null)
    {
        string joinedEmails = string.Join(", ", recepients.Select(x => x.Email));
        if (string.IsNullOrEmpty(joinedEmails))
        {
            _logger.LogWarning($"Requested mail is ignored due to empty recepients (subject:{subject}) (message:{message})");
            return;
        }
        if (_config.Disabled)
        {
            _logger.LogInformation($"[Disabled] Requesting email to {joinedEmails}");
            return;
        }
        _logger.LogInformation($"Requesting email to {joinedEmails}");
        var query = new MailingRequestQuery()
        {
            Recepients = recepients.ToList(),
            Subject = $"{subject} | PVSystem24.ru",
            Body = message,
            Offset = offset
        };
        var jsonData = JsonSerializer.Serialize(query);
        using var content = new StringContent(jsonData, MediaTypeHeaderValue.Parse("application/json"));
        using var client = _httpClientFactory.CreateClient();
        await client.PostAsync($"http://{_config.Host}:{_config.Port}/Send", content);
    }

    public async Task SendEmailAsync<TComponent>(IEnumerable<MailingRequestRecepient> recepients, string subject, ParameterView view, DateTimeOffset? offset = null) where TComponent : IComponent
    {
        var message = await _renderer.Dispatcher.InvokeAsync(async () =>
        {
            var messageComponent = await _renderer.RenderComponentAsync<TComponent>(view);
            return messageComponent.ToHtmlString();
        });
        await SendEmailAsync(recepients, subject, message, offset);
    }

    public async Task SendEmailAsync<TComponent>(IEnumerable<MailingRequestRecepient> recepients, string subject, DateTimeOffset? offset = null) where TComponent : IComponent
    {
        var message = await _renderer.Dispatcher.InvokeAsync(async () =>
        {
            var messageComponent = await _renderer.RenderComponentAsync<TComponent>();
            return messageComponent.ToHtmlString();
        });
        await SendEmailAsync(recepients, subject, message, offset);
    }

    public async Task SendEmailAsync<TComponent>(IEnumerable<MailingRequestRecepient> recepients, string subject, Dictionary<string, object?>? dictionary = null, DateTimeOffset? offset = null) where TComponent : IComponent
    {
        if (dictionary == null)
        {
            await SendEmailAsync<TComponent>(recepients, subject, offset);
            return;
        }
        var view = ParameterView.FromDictionary(dictionary);
        await SendEmailAsync<TComponent>(recepients, subject, view, offset);
    }
}

public sealed class MailingRequestRecepient
{
    public string? Name { get; set; }
    public string Email { get; set; } = null!;
    public MailingRequestRecepient(string? name, string email)
    {
        Email = email;
        Name = name;
    }
}

public sealed class MailingRequestQuery
{
    public List<MailingRequestRecepient> Recepients { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public DateTimeOffset? Offset { get; set; }
}