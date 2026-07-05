using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Core.Models;
using PartnerIntegration.BFF.Infrastructure.Options;
using RabbitMQ.Client;
using System.Text.Json;

namespace PartnerIntegration.BFF.Infrastructure.Publishers;

internal sealed class TransactionMessagePublisher(
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<TransactionMessagePublisher> logger) : ITransactionMessagePublisher
{
    private readonly string _queueName = options.Value.QueueName;

    public async Task PublishTransactionAsync(PartnerTransactionRequest transaction, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Publishing transaction {TransactionReference} to queue {QueueName}", transaction.TransactionReference, _queueName);

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

        logger.LogInformation("Transaction {TransactionReference} published successfully to queue {QueueName}", transaction.TransactionReference, _queueName);
    }
}
