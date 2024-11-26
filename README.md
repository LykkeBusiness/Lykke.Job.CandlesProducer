[![.NET](https://github.com/LykkeBusiness/Lykke.Job.CandlesProducer/actions/workflows/build.yml/badge.svg)](https://github.com/LykkeBusiness/Lykke.Job.CandlesProducer/actions/workflows/build.yml)

# Lykke.Job.CandlesProducer

## Description

Produces real-time candles updates, generated from the RabbitMq quotes exchange, into the candles exchange.

### Settings ###

Settings schema is:
<!-- MARKDOWN-AUTO-DOCS:START (CODE:src=./template.json) -->
<!-- The below code snippet is automatically added from ./template.json -->
```json
{
  "APP_UID": "Integer",
  "ASPNETCORE_ENVIRONMENT": "String",
  "Assets": {
    "ApiKey": "String",
    "CacheExpirationPeriod": "DateTime",
    "ServiceUrl": "String"
  },
  "ENVIRONMENT": "String",
  "ENV_INFO": "String",
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "String"
      }
    }
  },
  "MtCandlesProducerJob": {
    "ApiKey": "String",
    "AssetsCache": {
      "ExpirationPeriod": "DateTime"
    },
    "CandlesGenerator": {
      "GenerateBidAndAsk": "Boolean",
      "GenerateTrades": "Boolean",
      "OldDataWarningTimeout": "DateTime"
    },
    "Cqrs": {
      "ConnectionString": "String",
      "EnvironmentName": "String",
      "RetryDelay": "DateTime"
    },
    "Db": {
      "LogsConnString": "String",
      "SnapshotsConnectionString": "String",
      "StorageMode": "String"
    },
    "Rabbit": {
      "CandlesPublication": {
        "ConnectionString": "String",
        "Namespace": "String"
      },
      "QuotesSubscribtion": "String",
      "TradesSubscription": {
        "ConnectionString": "String"
      }
    },
    "SkipEodQuote": "Boolean",
    "UseSerilog": "Boolean"
  },
  "NOVA_FILTERED_MESSAGE_TYPES": "String",
  "serilog": {
    "minimumLevel": {
      "default": "String"
    }
  },
  "TZ": "String"
}
```
<!-- MARKDOWN-AUTO-DOCS:END -->
