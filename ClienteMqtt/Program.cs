using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Text.Json;


namespace ClienteMqtt
{
    public class ClienteMqtt : BackgroundService
    {
        private readonly ILogger<ClienteMqtt> _logger;
        private Guid ServiceGuid = Guid.NewGuid();
        private MqttFactory TelemetryFactory = new MqttFactory();

        public ClienteMqtt(ILogger<ClienteMqtt> logger)
        {
            _logger = logger;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {


            return base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            IMqttClient TelemetryClient = TelemetryFactory.CreateMqttClient();

            IMqttClientOptions ConnectionOptions = new MqttClientOptionsBuilder()
            .WithClientId("cliente")
            .WithTcpServer("localhost",1883)
            // .WithCredentials("admin", "public")
            .WithCleanSession()
            .Build();

            TelemetryClient.UseConnectedHandler(async e =>
            {
                _logger.LogInformation("-- Conectado a la telemetria mqtt --");
                
                await TelemetryClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("demo").Build());
                _logger.LogInformation("-- Subscrito a topic demo --");
            });

            TelemetryClient.UseApplicationMessageReceivedHandler(e =>
            {

                
                _logger.LogInformation("Mensaje recivido a: {time}", DateTimeOffset.Now);
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                

                // JsonDocument packageData = JsonDocument.Parse(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                // string data =packageData.RootElement.GetProperty("UUIDTimestamp").GetString();
                // TimeUuid dateUUID = TimeUuid.Parse(data);
                // DateTimeOffset dateUid = dateUUID.GetDate();
                // Console.WriteLine($"dateString:{data},UUID:{dateUUID.ToString()}, dateUUID:{dateUid} ");
                // Task.Run(() => mqttClient.PublishAsync("hello/world"));
            });

            TelemetryClient.UseDisconnectedHandler(async e =>
            {
                _logger.LogInformation("### Desconectado del servidor ###");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await TelemetryClient.ConnectAsync(ConnectionOptions, CancellationToken.None); // Since 3.0.5 with CancellationToken
                }
                catch
                {
                    _logger.LogInformation("### RECONNECTING FAILED ###");
                }
            });

            await TelemetryClient.ConnectAsync(ConnectionOptions, CancellationToken.None);


            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(2);
            var timer = new System.Threading.Timer(async (e) =>
            {
                var rand = new Random();
                int num = rand.Next(100); 

                var message = new MqttApplicationMessageBuilder()
                .WithTopic("demo")
                .WithPayload("{'temp':"+num+"}")
                .WithExactlyOnceQoS()
                .Build();
                await TelemetryClient.PublishAsync(message, CancellationToken.None);
            }, null, startTimeSpan, periodTimeSpan);


            // while (!stoppingToken.IsCancellationRequested)
            // {
            //     _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //     await Task.Delay(1000, stoppingToken);
            // }

            // Create a timer with a two second interval.
            // aTimer = new System.Timers.Timer(2000);
            // // Hook up the Elapsed event for the timer. 
            // aTimer.Elapsed += OnTimedEvent;
            // aTimer.AutoReset = true;
            // aTimer.Enabled = true;

        }
        // private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        // {
        //     Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
        //                       e.SignalTime);
        // }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }

}
