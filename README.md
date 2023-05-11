# Observability in ASP.NET Test API

This repository contains an ASP.NET Core Web API that demonstrates how to implement observability concepts, including tracing, logging, and metrics. The code demonstrates how to use the built-in
libraries in ASP.NET core to write observable code. Inspecting the testapi.csproj file, you'll 
see that no additional libraries are imported.

In this project, there is no integration with an observability SDK (for example, OpenTelemetry SDK). 
One can use auto-instrumentation to plug an SDK into this example and see how the signals
emitted from the application may be collected and analyzed.

The API accepts requests in the form of an `ApiCall` object, which represents a series of sub-calls to other services. The API executes these sub-calls asynchronously and returns a `Result` object that aggregates the results of the sub-calls.

The `ApiCall` payload looks like this in JSON:

```json
{
    "name": "api1-testapi",
    "calls": [
        {
            "name": "api2-testapi",
            "errors": 20
        },
        {
            "name": "api3-testapi",
            "latency": 2000,
            "calls": [
                {
                    "name": "api4-testapi"
                }
            ]
        }
    ]
}
```

## Getting Started

To run the API, you'll need [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0) or later installed. Once you've installed .NET, you can run the API using the following command:

```sh
dotnet run
```

The API listens on port 5000 by default. You can test the API using a tool like [curl](https://curl.se/) or [Postman](https://www.postman.com/).

### Running the API with Docker

You can also run the API using Docker. To build the Docker image, run the following command:

```sh
docker build -t test-api .
```

To run the Docker container, run the following command:

```sh
docker run -p 5000:80 test-api
```

## Observability

The example application implements the following observability concepts. 

### Tracing

The API uses [ActivitySource](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource) to generate trace spans for each API call and sub-call. The trace spans are linked together to form a trace that represents the entire operation. The API records the trace which can be collected and sent to a backend system, such as [Zipkin](https://zipkin.io/) or [Jaeger](https://www.jaegertracing.io/), for analysis.

### Logging

The API uses the built-in [ILogger](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0) interface to log messages at different severity levels. The API logs messages related to each API call and sub-call, including the number of sub-calls, any errors encountered, and the latency of the call.

### Metrics

The API uses [System.Diagnostics.Metrics](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) to generate metrics for the API as a whole. The API uses counters to track the number of sub-calls made by the API and histograms to track the response time of each sub-call.

## Contributing

If you find a bug or have a feature request, please [open an issue](https://github.com/<username>/<repo>/issues). Pull requests are welcome! For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).
