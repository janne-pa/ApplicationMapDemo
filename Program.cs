using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Azure.Storage.Blobs;
using System.Text;
using System.IO;
using dotenv.net;

namespace MyConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load environment variables from .env file
            DotEnv.Load();
            
            // Initialize Application Insights
            var telemetryConfig = TelemetryConfiguration.CreateDefault();
            telemetryConfig.ConnectionString = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING") 
                ?? throw new ArgumentNullException("APPLICATION_INSIGHTS_CONNECTION_STRING environment variable is not set");
            
            string blobStorageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                ?? throw new ArgumentNullException("AZURE_STORAGE_CONNECTION_STRING environment variable is not set");

            // Add dependency collector - required for Application Map
            var dependencyCollectorModule = new DependencyTrackingTelemetryModule();
            dependencyCollectorModule.Initialize(telemetryConfig);

            // Configure QuickPulse (Live Metrics)
            var quickPulseModule = new QuickPulseTelemetryModule();
            quickPulseModule.Initialize(telemetryConfig);

            telemetryConfig.TelemetryProcessorChainBuilder
                .Use((next) => new QuickPulseTelemetryProcessor(next))
                .Build();

            // Initialize TelemetryClient
            var telemetryClient = new TelemetryClient(telemetryConfig);
            telemetryClient.Context.Cloud.RoleName = "JPConsoleApp";
            telemetryClient.Context.Cloud.RoleInstance = Environment.MachineName;
            telemetryClient.Context.Operation.Id = Guid.NewGuid().ToString(); // Add operation context

            Console.WriteLine($"Starting {telemetryClient.Context.Cloud.RoleName} on {telemetryClient.Context.Cloud.RoleInstance}");
            Console.WriteLine("Press Ctrl+C to exit.");

            using var httpClient = new HttpClient();
            int count = 1;

            while (true)
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    var response = await httpClient.GetAsync("https://www.microsoft.com");

                    // Initialize blob client
                    var blobServiceClient = new BlobServiceClient(blobStorageConnectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient("httpstatus");
                    await containerClient.CreateIfNotExistsAsync();

                    // Upload to blob with operation tracking
                    var blobName = $"status-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.txt";
                    var blobClient = containerClient.GetBlobClient(blobName);

                    var content = $"Status: {response.StatusCode}\nTimestamp: {DateTime.UtcNow}";
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

                    // #1 Track blob storage dependency explicitly
                    telemetryClient.TrackDependency(
                        dependencyTypeName: "Azure Blob",
                        target: containerClient.Uri.Host,
                        dependencyName: $"Upload {blobName}",
                        data: blobClient.Uri.ToString(),
                        startTime: startTime,
                        duration: DateTime.UtcNow - startTime,
                        resultCode: "200",
                        success: true);

                    await blobClient.UploadAsync(stream, true);

                    // #2 Track dependency with operation context
                    telemetryClient.TrackDependency(
                        dependencyTypeName: "HTTP",
                        target: "microsoft.com",
                        dependencyName: "GET /",
                        data: "https://www.microsoft.com",
                        startTime: DateTimeOffset.UtcNow,
                        duration: TimeSpan.FromMilliseconds(response.Headers.Date.HasValue
                            ? (DateTime.UtcNow - response.Headers.Date.Value.UtcDateTime).TotalMilliseconds
                            : 0),
                        resultCode: response.StatusCode.ToString(),
                        success: response.IsSuccessStatusCode);

                    // #3 Do heartbeat tracking
                    telemetryClient.TrackEvent("HeartbeatEvent");

                    // Print every 10th heartbeat
                    if (count % 10 == 0)
                    {
                        Console.WriteLine($"Heartbeat #{count} sent - HTTP Status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    Console.WriteLine($"Error: {ex.Message}");
                }

                count++;
                await Task.Delay(1000);
            }
        }
    }
}