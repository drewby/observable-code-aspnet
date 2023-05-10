using System.Diagnostics;
using System.Diagnostics.Metrics;

// Gets the application name, version, revision, and build time.
var appInfo = AppInfo.GetAppInfo();

// If the --version argument is passed, print the application
// name, version, revision, and build time and exit. This
// makes it easy to see the version of the application running
// in a cluster.
if (args.Contains("--version"))
{
    Console.WriteLine($"{appInfo.Name}\n{appInfo.Version}\n{appInfo.Revision}\n{appInfo.BuildTime}");
    return 0;
}

var app = WebApplication.Create(args);

// Create an ActivitySource for the application. An ActivitySource
// is a class that generates Activity objects and has a name. The
// name should be the same across all instances of the application.
using var activitySource = new ActivitySource(appInfo.Name);

// Create a Meter for the application. A Meter is a class that
// generates metrics and has a name. The name should be the same
// across all instances of the application.
using var meter = new Meter(appInfo.Name);
var subCallsCounter = meter.CreateCounter<int>("subCalls");

// Simple health endpoint simple returns "OK" if the process is alive.
app.MapGet("/health", () => "OK");

// The /version endpoint returns the application name, version, revision, and build time.
// This makes it easy to see the version of the application running in a cluster.
app.MapGet("/version", () => $"App Name: {appInfo.Name}, Version: {appInfo.Version}, Revision: {appInfo.Revision}, BuildTime: {appInfo.BuildTime}");

// The /test endpoint accepts payload which represents the requested
// actions for this instance of the testapi.
app.MapPost("/test", async (ApiCall call, HttpRequest request) =>
{
    // Create a new Activity object representing the current API call,
    // and attach it to the current context.
    //
    // An Activity is a representation of a unit of work performed by
    // the application. It has a name, start time, duration, and
    // a set of key-value pairs called tags, which record information
    // about the work. Each Activity also records relationships to
    // other Activities, known as links, and causality, known as
    // parent-child relationship.
    using var activity = activitySource.StartActivity(ActivityKind.Server, name: "API TEST",
        tags: new Dictionary<string, object?>
        {
            ["call.subCalls.Count"] = call.Calls.Count,
            ["call.withErrors"] = call.Errors,
            ["call.withLatency"] = call.Latency
        });


    // Baggage is a set of key-value pairs stored on the Activity.
    // Baggage is propagated to all children of the Activity.
    // Baggage is used to propagate information in-band across process
    // boundaries.
    //
    // If the request contains baggage, add it to an Event on the
    // Activity. If the request does not contain baggage, add the
    // User-Agent header to the Activity's baggage.
    if (activity?.Baggage.Any() ?? false)
    {
        var baggage = string.Join(",", activity.Baggage.Select(b => $"{b.Key}={b.Value}"));
        activity?.AddEvent(new ActivityEvent($"Found baggage: {baggage}"));
    }
    else if (request.Headers.TryGetValue("User-Agent", out var userAgent))
    {
        activity?.AddBaggage("User-Agent", userAgent);
    }

    // Update the meter Counter by adding the number of subCalls.
    subCallsCounter.Add(call.Calls.Count);

    // Log information using the built-in ILogger in ASP.NET
    // core. When we later instrument the application, additional
    // information will be added to the log entry. OpenTelemetry
    // for example will include the traceId and spanId of the current
    // Activity.
    app.Logger.LogInformation($"Test called with {call.Calls.Count} subCalls, {call.Errors}% errors, and {call.Latency}ms latency");

    // Execute the call and return the result.
    return Microsoft.AspNetCore.Http.Results.Ok(await call.Execute());
});
app.Run();
return 0;
