﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Common.Log;

using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lykke.Job.CandlesProducer.Services;

public class RabbitPoisonHandlingService<T> : IRabbitPoisonHandlingService, IDisposable where T : class
{
    private readonly ILog _log;
    private readonly RabbitMqSubscriptionSettings _subscriptionSettings;

    private readonly JsonMessageDeserializer<T> _messageDeserializer = new();
    private readonly JsonMessageSerializer<T> _messageSerializer = new();

    private readonly List<IModel> _channels = [];
    private IConnection _connection;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private string PoisonQueueName => $"{_subscriptionSettings.QueueName}-poison";

    public RabbitPoisonHandlingService(
        ILog log,
        RabbitMqSubscriptionSettings subscriptionSettings)
    {
        _log = log;
        _subscriptionSettings = subscriptionSettings;
    }

    public async Task<string> PutMessagesBack()
    {
        if (_semaphoreSlim.CurrentCount == 0)
        {
            throw new ProcessAlreadyStartedException("Cannot start the process because it was already started and not yet finished.");
        }

        await _semaphoreSlim.WaitAsync(TimeSpan.FromMinutes(10));

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_subscriptionSettings.ConnectionString) };
            await _log.WriteInfoAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack),
                $"Trying to connect to {factory.Endpoint} ({_subscriptionSettings.ExchangeName})");

            _connection = factory.CreateConnection();

            var originalQueueChannel = _connection.CreateModel();
            var poisonQueueChannel = _connection.CreateModel();
            _channels.AddRange([originalQueueChannel, poisonQueueChannel]);

            var queueDeclareOk = poisonQueueChannel.QueueDeclarePassive(PoisonQueueName);

            var processedMessages = 0;
            string result;

            if (queueDeclareOk.MessageCount == 0)
            {
                result = "No messages found in poison queue. Terminating the process.";

                await _log.WriteWarningAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack),
                    $"No messages found in poison queue. Terminating the process.");
                FreeResources();
                return result;
            }
            else
            {
                await _log.WriteInfoAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack),
                    $"{queueDeclareOk.MessageCount} messages found in poison queue. Starting the process.");
            }

            originalQueueChannel.QueueDeclarePassive(_subscriptionSettings.QueueName);

            var consumer = new EventingBasicConsumer(poisonQueueChannel);
            consumer.Received += (ch, ea) =>
            {
                var message = RepackMessage(ea.Body.ToArray());

                if (message != null)
                {
                    try
                    {
                        IBasicProperties properties = null;
                        if (!string.IsNullOrEmpty(_subscriptionSettings.RoutingKey))
                        {
                            properties = originalQueueChannel.CreateBasicProperties();
                            properties.Type = _subscriptionSettings.RoutingKey;
                        }

                        originalQueueChannel.BasicPublish(_subscriptionSettings.ExchangeName,
                            _subscriptionSettings.RoutingKey ?? "", properties, message);

                        poisonQueueChannel.BasicAck(ea.DeliveryTag, false);

                        processedMessages++;
                    }
                    catch (Exception e)
                    {
                        _log.WriteErrorAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack), $"Error resending message: {e.Message}", e);
                    }
                }
            };

            var sw = new Stopwatch();
            sw.Start();

            var tag = poisonQueueChannel.BasicConsume(PoisonQueueName, false,
                consumer);

            await _log.WriteInfoAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack),
                $"Consumer {tag} started.");

            while (processedMessages < queueDeclareOk.MessageCount)
            {
                Thread.Sleep(100);

                if (sw.ElapsedMilliseconds > 30000)
                {
                    await _log.WriteWarningAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack),
                        $"Messages resend takes more than 30s. Terminating the process.");

                    break;
                }
            }

            result = $"Messages resend finished. Initial number of messages {queueDeclareOk.MessageCount}. Processed number of messages {processedMessages}";

            await _log.WriteInfoAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack), result);

            FreeResources();

            return result;
        }
        catch (Exception exception)
        {
            var result =
                $"Exception [{exception.Message}] thrown while putting messages back from poison to queue {_subscriptionSettings.QueueName}. Stopping the process.";

            await _log.WriteErrorAsync(nameof(RabbitPoisonHandlingService<T>), nameof(PutMessagesBack), result, exception);

            return result;
        }
        finally
        {
            FreeResources();
        }
    }

    private void FreeResources()
    {
        foreach (var channel in _channels)
        {
            channel?.Close();
            channel?.Dispose();
        }
        _connection?.Close();
        _connection?.Dispose();

        _semaphoreSlim.Release();

        _log.WriteInfo(nameof(RabbitPoisonHandlingService<T>), nameof(FreeResources), $"Channels and connection disposed.");
    }

    public void Dispose()
    {
        FreeResources();
    }

    private byte[] RepackMessage(byte[] serializedMessage)
    {
        T message;
        try
        {
            message = _messageDeserializer.Deserialize(serializedMessage);
        }
        catch (Exception exception)
        {
            _log.WriteErrorAsync(this.GetType().Name, nameof(RepackMessage),
                $"Failed to deserialize the message: {serializedMessage} with {_messageDeserializer.GetType().Name}. Stopping.",
                exception).GetAwaiter().GetResult();
            return null;
        }

        return _messageSerializer.Serialize(message);
    }
}
