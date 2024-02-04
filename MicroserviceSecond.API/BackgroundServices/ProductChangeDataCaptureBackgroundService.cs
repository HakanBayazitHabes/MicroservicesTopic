﻿using System.Text.Json;
using System.Text.Json.Nodes;
using Confluent.Kafka;
using MicroserviceSecond.API.Models;
using static Confluent.Kafka.ConfigPropertyNames;

namespace MicroserviceSecond.API.BackgroundServices
{
    public class ProductChangeDataCaptureBackgroundService : BackgroundService
    {
        private IConsumer<Ignore, string>? _consumer;

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9094",
                GroupId = "mygroup-1",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            _consumer.Subscribe("fullfillment.ChangeDataCaptureDb.dbo.Products");

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer!.Consume();

                var message = consumeResult.Message.Value;


                #region 2. way

                var productChangeDataCapture =
                    JsonSerializer.Deserialize<ProductChangeDataCaptureModel2>(message,
                        new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true,
                            IncludeFields = true
                        });

                var product = productChangeDataCapture.Payload.After;
                var op = productChangeDataCapture.Payload.Op;
                Console.WriteLine($"Operation: {op}, Product: {product.Id} - {product.Name} - {product.Price}");

                #endregion


                #region 1. way

                //var jsonNode = JsonNode.Parse(consumeResult.Message.Value);

                //var payload = jsonNode?["payload"]!;
                //var after = payload?["after"]!;

                //var op = payload?["op"]!.GetValue<string>();


                ////satırdaki ‘op’ alanındaki c, u, d ve r değerleri ifade etmektedir.

                //var product = new ProductChangeDataCaptureModel
                //{
                //    Id = after["Id"]!.GetValue<int>(),
                //    Name = after["Name"]!.GetValue<string>()
                //};
                //var priceAsString = after["Price"]?.GetValue<string>();


                //if (!string.IsNullOrEmpty(priceAsString))
                //{
                //    product.Price = decimal.Parse(priceAsString);
                //}

                //Console.WriteLine($"Operation: {opt}, Product: {product.Id} - {product.Name} - {product.Price}"); 

                #endregion

                _consumer.Commit(consumeResult);


                await Task.Delay(100, stoppingToken);
            }
        }
    }
}