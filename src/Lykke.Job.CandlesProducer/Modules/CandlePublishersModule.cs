// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;

using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Services;
using Lykke.Job.CandlesProducer.Services.Candles;
using Lykke.Job.CandlesProducer.Settings;

using MarginTrading.AssetService.Contracts.Candles;

namespace Lykke.Job.CandlesProducer.Modules
{
    public class CandlePublishersModule : Module
    {
        private readonly CandlesPublicationRabbitSettings _candlesPublicationRabbitSettings;
        private readonly CandlesProducerSettingsContract _candlesProducerSettings;
        
        public CandlePublishersModule(
            CandlesPublicationRabbitSettings candlesPublicationRabbitSettings, 
            CandlesProducerSettingsContract candlesProducerSettings)
        {
            _candlesPublicationRabbitSettings = candlesPublicationRabbitSettings;
            _candlesProducerSettings = candlesProducerSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RabbitMqPublishersFactory>()
                .As<IRabbitMqPublishersFactory>();

            builder.RegisterType<DefaultCandlesPublisher>()
                .As<IDefaultCandlesPublisher>()
                .SingleInstance()
                .WithParameter("connectionString", _candlesPublicationRabbitSettings.ConnectionString)
                .WithParameter("nspace", _candlesPublicationRabbitSettings.Namespace)
                .WithParameter("shardName", _candlesProducerSettings.DefaultShardName);

            foreach (var shard in _candlesProducerSettings.Shards)
            {
                if (shard.Name == _candlesProducerSettings.DefaultShardName)
                    throw new InvalidOperationException(
                        $"The shard name [{_candlesProducerSettings.DefaultShardName}] can't be used since it is already in use by default candles publisher. Please rename.");

                builder.RegisterType<CandlesPublisher>()
                    .As<ICandlesPublisher>()
                    .SingleInstance()
                    .WithParameter("connectionString", _candlesPublicationRabbitSettings.ConnectionString)
                    .WithParameter("nspace", _candlesPublicationRabbitSettings.Namespace)
                    .WithParameter("shardName", shard.Name)
                    .WithParameter("shardPattern", shard.Pattern);
            }

            builder.RegisterType<CandlesPublisherProvider>()
                .As<ICandlesPublisherProvider>()
                .SingleInstance();
        }
    }
}
