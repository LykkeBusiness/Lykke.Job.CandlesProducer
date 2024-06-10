using Lykke.RabbitMqBroker;

namespace Lykke.Job.CandlesProducer.Services.Helpers
{
    public static class RabbitMqSubscriptionSettingsHelper
    {
        public static RabbitMqSubscriptionSettings GetSubscriptionSettings(string connectionString, string @namespace, string source, string queueSuffix = null) =>
            RabbitMqSubscriptionSettings
                .CreateForSubscriber(connectionString, @namespace, source, @namespace, $"candlesproducer{queueSuffix}")
                .MakeDurable();
    }
}
