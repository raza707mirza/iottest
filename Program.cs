using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOTClientTest
{
    class Program
    {
        private DeviceClient _client;
        static async Task Main(string[] args)
        {
            
            await new Program().RunAsync(collection =>
            {
                return null;
            }, (new CancellationTokenSource()).Token);
            Console.Read();
        }
        
        public async Task RunAsync(Func<TwinCollection, Task<TwinCollection>> twinUpdateHandler, CancellationToken cancellationToken)
        {
            var settings = new ITransportSettings[]
            {
                new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                {
                    KeepAliveInSeconds = 10,
                }
            };

            _client = DeviceClient.CreateFromConnectionString(
                "HostName=XXXX;SharedAccessKey=XXXXX",
                "XXXX", settings);
            
            
            var retryPolicy = new ExponentialBackoff(5,
                TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(120),
                TimeSpan.FromSeconds(5));
            _client.SetRetryPolicy(retryPolicy);

            
            _client.SetConnectionStatusChangesHandler((status, reason) =>
            {
                Console.WriteLine($"IoT Hub connection status Changed Status: {status} Reason: {reason} Time: {DateTime.Now.ToString("h:mm:ss tt zz")}");
            });
            
             await _client.OpenAsync();
            

            await _client.SetMethodHandlerAsync("executeShell", async (req, context) =>
            {
                await Task.Delay(0);
                return new MethodResponse(500);
            },null);

            await _client.SetMethodDefaultHandlerAsync(MethodHandler, null);
            
            await _client.SetDesiredPropertyUpdateCallbackAsync(async (collection, context) =>
                {
                    var updated = await twinUpdateHandler(collection);
                    await _client.UpdateReportedPropertiesAsync(updated);
                }
                , null);

            await ReceiveMessagesAsync(cancellationToken);
        }

        private async Task<MethodResponse> MethodHandler(MethodRequest methodRequest, object parameter)
        {
            await Task.Delay(0);
            return new MethodResponse(500);
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _client.ReceiveAsync(TimeSpan.FromSeconds(10));
                if (message != null)
                {
                    //Do something with received message...
                }
            }
        }
        
    }
}
