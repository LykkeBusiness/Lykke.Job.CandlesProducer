## 2.10.0 - Nova 2. Delivery 47 (November 15, 2024)
### What's changed
* LT-5854: Update messagepack to 2.x version.
* LT-5783: Add assembly load logger.
* LT-5758: Migrate to quorum queues.
  
### Deployment
In this release, all previously specified queues have been converted to quorum queues to enhance system reliability. The affected queues are:
- `lykke.mt.pricefeed.candlesproducer`
- `lykke.mt.trades.candlesproducer-v2`

#### Automatic Conversion to Quorum Queues
The conversion to quorum queues will occur automatically upon service startup **if**:
* There are **no messages** in the existing queues.
* There are **no active** subscribers to the queues.

**Warning**: If messages or subscribers are present, the automatic conversion will fail. In such cases, please perform the following steps:
1. Run the previous version of the component associated with the queue.
1. Make sure all the messages are processed and the queue is empty.
1. Shut down the component associated with the queue.
1. Manually delete the existing classic queue from the RabbitMQ server.
1. Restart the component to allow it to create the quorum queue automatically.

#### Poison Queues
All the above is also applicable to the poison queues associated with the affected queues. Please ensure that the poison queues are also converted to quorum queues.

#### Disabling Mirroring Policies
Since quorum queues inherently provide data replication and reliability, server-side mirroring policies are no longer necessary for these queues. Please disable any existing mirroring policies applied to them to prevent redundant configurations and potential conflicts.

#### Environment and Instance Identifiers
Please note that the queue names may include environment-specific identifiers (e.g., dev, test, prod). Ensure you replace these placeholders with the actual environment names relevant to your deployment. The same applies to instance names embedded within the queue names (e.g., DefaultEnv, etc.).


## 2.9.0 - Nova 2. Delivery 46 (September 26, 2024)
### What's changed
* LT-5603: Migrate to net 8.


## 2.8.0 - Nova 2. Delivery 45 (September 02, 2024)
### What's changed
* LT-5565: Implement rfactor saga.

### Deployment
Add a new `Cqrs` section next to `AssetsCache` (`Cqrs` connection string should point to the broker-level rabbitmq instance)
Example
```
    "AssetsCache": 
    {
      "ExpirationPeriod": "00:05:00",
      "ApiKey": "apikey"
    },
    "Cqrs": 
    {
      "ConnectionString": "amqp://login:password@rabbit-mt.mt.svc.cluster.local:5672",
      "RetryDelay": "00:00:02",
      "EnvironmentName": "dev"
    },
```


## 2.7.0 - Nova 2. Delivery 44 (August 15, 2024)
### What's changed
* LT-5518: Update rabbitmq broker library with new rabbitmq.client and templates.

### Deployment
Please ensure that the mirroring policy is configured on the RabbitMQ server side for the following queues:
- `lykke.mt.pricefeed.candlesproducer`
- `lykke.mt.trades.candlesproducer-v2`

These queues require the mirroring policy to be enabled as part of our ongoing initiative to enhance system reliability. They are now classified as "no loss" queues, which necessitates proper configuration. The mirroring feature must be enabled on the RabbitMQ server side.

In some cases, you may encounter an error indicating that the server-side configuration of a queue differs from the client’s expected configuration. If this occurs, please delete the queue, allowing it to be automatically recreated by the client.

**Warning 1**: The "no loss" configuration is only valid if the mirroring policy is enabled on the server side.

**Warning 2**: The particular queue might or might not exist in your environment depending on the configuration. 

Please be aware that the provided queue names may include environment-specific identifiers (e.g., dev, test, prod). Be sure to replace these with the actual environment name in use. The same applies to instance names embedded within the queue names (e.g., DefaultEnv, etc.).


## 2.6.0 - Nova 2. Delivery 41 (March 29, 2024)
### What's changed
* LT-5446: Update packages.


## 2.5.0 - Nova 2. Delivery 40 (February 28, 2024)
### What's changed
* LT-5291: Step: update version number is failed.
* LT-5201: Update lykke.httpclientgenerator to 5.6.2.


## 2.4.0 - Nova 2. Delivery 39 (January 29, 2024)
### What's changed
* LT-5170: Add history of releases into `changelog.md`


## 2.3.0 - Nova 2. Delivery 36 (2023-08-31)
### What's changed
* LT-4895: Update nugets.


**Full change log**: https://github.com/lykkecloud/lykke.job.candlesproducer/compare/v2.2.0...v2.3.0

## v2.2.0 - Nova 2. Delivery 35
## What's changed
* LT-4802: R-factor chart update (quick fix).


**Full change log**: https://github.com/lykkecloud/lykke.job.candlesproducer/compare/v2.1.0...v2.2.0

## v2.1.0 - Nova 2. Delivery 28
## What's Changed
* LT-3721: NET 6 migration

### Deployment
* NET 6 runtime is required
* Dockerfile is updated to use native Microsoft images (see [DockerHub](https://hub.docker.com/_/microsoft-dotnet-runtime/))

**Full Changelog**: https://github.com/LykkeBusiness/Lykke.Job.CandlesProducer/compare/v2.0.3...v2.1.0

## v2.0.3 - Nova 2. Delivery 24
## What's Changed
* LT-3896: [CandlesProducer] Upgrade Lykke.HttpClientGenerator nuget by @lykke-vashetsin in https://github.com/LykkeBusiness/Lykke.Job.CandlesProducer/pull/30

## New Contributors
* @lykke-vashetsin made their first contribution in https://github.com/LykkeBusiness/Lykke.Job.CandlesProducer/pull/29

**Full Changelog**: https://github.com/LykkeBusiness/Lykke.Job.CandlesProducer/compare/v2.0.1...v2.0.3

## v2.0.2 - Nova 2. Delivery 21
* LT-3717: NOVA security threats

## v1.4.2 - Sharding
### Tasks

* LT-2338:  Implement candles sharding based on asset pair regexp

### Deployment

* Requires Settings service v1.16.2

### Description

The producer will be publishing candles by default route if no sharding settings applied. 

The default exchange name is lykke.mt.candles-v2.default but in fact, the prefix depends on service settings.

The example of sharding settings:

````json
"CandlesSharding" : {
  "Shards": [
    {
      "Name": "vera_assets_candles",
      "Pattern": "vera"
    }
  ],
  "DefaultShardName": "whatever"
}
````
The above configuration will make all the candles where asset id contains vera to be published to the following exchange: lykke.mt.candles-v2.vera_assets_candles. Pattern field is regular expression. All candles, where asset has not been matched to any registered sharding pattern will go to default exchange which is lykke.mt.candles-v2.whatever for this configuration. 

Please note, all the sharding settings are optional. If nothing was set (including default shard name), all the candle messages will go to default exchange, which is lykke.mt.candles-v2.default.

The Shards section is intended to contain custom shards only. Default shard settings should not be added at all. All the candles not matching any shard will go to default exchange in any way.

In case, asset id satisfies multiple regular expressions (thus, multiple shards), the candle will go to default exchange. 

Warning: The key value of CandlesSharding.Shards[i].Name will be used as a part of exchange and queue names. Please do use allowed symbols only.

## v1.4.0 - Migration to .NET 3.1
### Tasks

LT-2180: Migrate to 3.1 Core and update DL libraries

## v1.3.11 - Bugfixes
### Tasks

LT-2012: Improve Alpine docker files
LT-2159: Fix threads leak with RabbitMq subscribers and publishers

## v1.3.8 - Improvements
### Tasks

LT-1986: Migrate to Alpine docker images in MT Core services
LT-1944: Message resend API for not critical brokers

## v1.3.6 - Fix first candle + extended logs
### Tasks 

LT-1901: BE: wrong open price
LT-1918: Provide configuration for Microsoft logs in Mt-core services (relates to MT-1852)

## v1.3.5 - Remove EOD candle
### Tasks

LT-1881: CR [Chart] EOD candle displayed on chart (related to LT-1862)

### Deployment

New settings added to "MtCandlesProducerJob":
````json
"SkipEodQuote": true
````
If true - skips all the EOD quotes => no EOD candles. True by default. 

## v1.3.4 - Extended logs
### Tasks

BUGS-1263: PROD CRITICAL 1D - Many FX Pair charts are totally wrong

## v1.3.3 - Swagger fix + .net update to 2.2
### Tasks

LT-1785: Error on swagger ui (right bottom corner)
LT-1765: update .net version to 2.2

## v1.3.0 - License
### Tasks
LT-1541: Update licenses in lego service to be up to latest requirements

## v1.2.2 - Secured API clients
### Tasks
MTC-809: Secure all "visible" endpoints in mt-core

### Deployment
Add new property ApiKey to Assets section (optional, if settings service does not use API key):
```json
"Assets": 
  {
    "ServiceUrl": "settings service url",
    "CacheExpirationPeriod": "00:05:00",
    "ApiKey": "settings service secret key"
  },
```

## v1.2.1 - Optimizations
### Tasks

MTC-781: Optimize candles

### Details

New settings (with default values) are added to CandlesGenerator section:
[Optional] public bool GenerateBidAndAsk { get; set; }
[Optional] public bool GenerateTrades { get; set; }
[Optional] public CandleTimeInterval[] TimeIntervals { get; set; } = new[]
{ CandleTimeInterval.Minute, CandleTimeInterval.Min5, CandleTimeInterval.Min15, CandleTimeInterval.Min30, CandleTimeInterval.Hour, CandleTimeInterval.Hour4, CandleTimeInterval.Day, CandleTimeInterval.Week, CandleTimeInterval.Month };

For the current project we need only Mid prices and only configured time intervals, so by default generation of Bid, Ask and Trade candles is disabled. Also Sec interval was disabled.

## v1.2.0 - Remove redundant SQL queries from CandlesProducer
### Tasks:
MTC-704: Remove redundant SQL queries from CandlesProducer

## v1.1.3 - Updated projects versions
No changes

## v1.1.2 - Logs bugfix
### Bugfixes
- Fixed Serilog text logs (MTC-461)

## v1.1.1 - Deployment and maintenance improvements Edit
Introduced:
Text logs

New settings:
UseSerilog": true

## v1.1.0 - Bug fixes
No config changes

## v1.0.5 - No Azure dependencies, SQL
[Configs.zip](https://github.com/lykkecloud/Lykke.Job.CandlesProducer/files/2251253/Configs.zip)
