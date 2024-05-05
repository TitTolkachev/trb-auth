using Confluent.Kafka;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using trb_auth.Common;
using trb_auth.Models;

namespace trb_auth.Services;

public class TransactionHandler : BackgroundService
{
    private readonly ILogger<TransactionHandler> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public TransactionHandler(ILogger<TransactionHandler> logger, ApplicationDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        Console.WriteLine("TransactionHandler Init");
        _logger = logger;
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var conf = new ConsumerConfig
        {
            GroupId = "trb-auth",
            BootstrapServers = Constants.KafkaHost,
            AutoOffsetReset = AutoOffsetReset.Latest
        };

        var consumer = new ConsumerBuilder<Ignore, string>(conf).Build();
        consumer.Subscribe("transaction.callback");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            Console.WriteLine("TransactionHandler Started");
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await Task.Run(() => consumer.Consume());

                await HandleMessage(message.Message.Value);

                _logger.LogInformation("Consumed message: {MessageValue}", message.Message.Value);
            }
        }
        catch (ConsumeException e)
        {
            _logger.LogInformation("Error occured: {Error}", e.Error.Reason);
        }
    }

    private async Task HandleMessage(string message)
    {
        var parsedMessage = JsonConvert.DeserializeObject<KafkaMessage>(message);

        if (parsedMessage == null)
            return;

        var payerId = await GetUserId(parsedMessage.Transaction?.PayerAccountId);
        var payeeId = await GetUserId(parsedMessage.Transaction?.PayeeAccountId);

        var devices = await _context.Devices
            .Where(device => device.App != "client" || device.UserId == payerId || device.UserId == payeeId)
            .ToListAsync();

        if (devices.Count == 0)
            return;

        var title = $"New Transaction: {parsedMessage.State}";
        var body = parsedMessage.Transaction == null
            ? ""
            : $"{parsedMessage.Transaction.Type} {parsedMessage.Transaction.Amount} {parsedMessage.Transaction.Currency}";

        var cloudMessage = new MulticastMessage
        {
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Tokens = devices.ConvertAll(device => device.DeviceId)
        };

        await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(cloudMessage);
    }

    private async Task<string?> GetUserId(string? accountId)
    {
        var httpClient = _httpClientFactory.CreateClient(Constants.CoreHttpClient);

        var response = await httpClient.GetAsync($"accounts/{accountId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("GetAccountInfo FAILED: {Response}", response.ToString());
            return null;
        }

        var account = await response.Content.ReadFromJsonAsync<Account>();

        return account?.ExternalClientId;
    }
}