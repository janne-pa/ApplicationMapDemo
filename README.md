# Application Insights Monitoring Demo

A demonstration project showing Application Insights integration with both a C# console application and a browser-based HTML application. This project helps visualize how different application components appear in the Application Map.

![Application Map](./application-map-snapshot.png)

## Project Components

### 1. Console Application
A background service that performs periodic health checks and stores results in Azure storage.

### 2. Web Demo Page (OnePageTelemetry.html)
An interactive HTML page demonstrating different types of Application Insights telemetry.

## Features

- **Console Application**:
  - HTTP status monitoring of microsoft.com
  - Azure Blob Storage integration for status logging
  - Operation context tracking
  - Custom event generation

- **Web Demo Page**:
  - Custom event tracking
  - Exception monitoring
  - Custom metric recording
  - Dependency tracking
  - Interactive visual guides for finding data in Azure Portal

## Prerequisites

- Azure subscription
- Application Insights resource
- Azure Storage account
- .NET 6.0 or later

## Configuration

### Console Application
Add a `.env` file with the following connection strings:

```
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=<your-key>;IngestionEndpoint=https://<location>.in.applicationinsights.azure.com/
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=<storage-name>;AccountKey=<storage-key>;EndpointSuffix=core.windows.net
```

### Web Demo Page
Update the `instrumentationKey` in `OnePageTelemetry.html` with your Application Insights key.

## Monitoring Features

- **Console Application**:
  - Live Metrics: View real-time performance in Azure Portal
  - Application Map: Visualize dependencies:
    - HTTP calls to microsoft.com
    - Blob storage operations
  - Custom Events: "HeartbeatEvent" tracked every second
  - Dependencies: Tracked for both HTTP and Blob operations
  - Exceptions: Automatic tracking of any runtime errors

- **Web Demo Page**:
  - Custom Events: Triggered by user interactions
  - Exceptions: Simulated errors for testing
  - Metrics: Custom metrics for user actions
  - Dependencies: Tracked for external API calls

## Output

- **Console Application**:
  - Console displays heartbeat status every 10 operations
  - Blob storage receives status files with format: `status-YYYY-MM-DD-HH-mm-ss.txt`
  - Application Insights receives:
    - Custom events
    - Dependency calls
    - Exceptions
    - Performance metrics

- **Web Demo Page**:
  - Application Insights receives:
    - Custom events
    - Exceptions
    - Metrics
    - Dependency calls

## Local Development

1. Clone the repository
2. Set up connection strings
3. Run the console application:
   ```powershell
   dotnet run
   ```
4. Open `OnePageTelemetry.html` in a browser for the web demo.

## Viewing Results

1. **Azure Portal - Application Insights**
   - Live Metrics: Immediate performance view
   - Application Map: Dependency visualization
   - Logs: Detailed telemetry data

2. **Azure Storage Explorer**
   - Container: "httpstatus"
   - Files: Timestamped status reports

3. **Web Demo Page**
   - Open browser console to view telemetry logs.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.