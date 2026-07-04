using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Core.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace PartnerIntegration.BFF.Infrastructure.Publishers;

internal class TransactionMessagePublisher : ITransactionMessagePublisher
{
    private readonly string _rabbitMqUri;
    private readonly string _queueName;
    private readonly ILogger<TransactionMessagePublisher> _logger;

    public TransactionMessagePublisher(IConfiguration configuration, ILogger<TransactionMessagePublisher> logger)
    {
        _logger = logger;

        _rabbitMqUri = configuration["RabbitMQ:Uri"] ?? throw new InvalidOperationException("RabbitMQ:Uri is not configured.");

        var queueName = configuration["RabbitMQ:QueueName"] ?? throw new InvalidOperationException("RabbitMQ:QueueName is not configured.");

        _queueName = queueName.Trim();
    }

    public async Task PublishTransactionAsync(PartnerTransactionRequest transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending transaction to queue {QueueName}: {TransactionReference}", _queueName, transaction.TransactionReference);

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_rabbitMqUri),
                AutomaticRecoveryEnabled = true
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(transaction);
            var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Transaction sent successfully to queue {QueueName}: {TransactionReference}", _queueName, transaction.TransactionReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send transaction to queue {QueueName}: {TransactionReference}", _queueName, transaction.TransactionReference);
            throw;
        }
    }
}
