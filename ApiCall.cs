
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;

public class Result
{
    public string? Name { get; set; }
    public int? StatusCode { get; set; }
    public List<Result> Calls { get; set; } = new List<Result>();
}

public class ApiCall
{
    public string? Name { get; set; }
    public List<ApiCall> Calls { get; set; } = new List<ApiCall>();
    public int? Errors { get; set; }
    public int? Latency { get; set; }

    internal async Task<Result> Execute()
    {
        var activity = Activity.Current;

        // Get env variable for template of subCall endpoint
        var urlTemplate = Environment.GetEnvironmentVariable("TEST_URL_TEMPLATE") ?? "http://{name}:5000/test";

        // For each subCall, make a call to the test endpoint of the named service
        var tasks = new List<Task<HttpResponseMessage>>();
        foreach (var subCall in this.Calls)
        {
            var url = urlTemplate.Replace("{name}", subCall.Name);
            activity?.AddEvent(new ActivityEvent($"Task added: Call {subCall.Name} at {url}"));
            tasks.Add(new HttpClient().PostAsJsonAsync(url, subCall));
        }
        await Task.WhenAll(tasks);

        // Throw a random exception if there is ApiCall.Errors value
        if (new Random().Next(100) < (this.Errors ?? 0))
        {
            throw new Exception("Random exception");
        }

        // Sleep for the length of ApiCall.Latency
        Thread.Sleep(this.Latency ?? 0);

        // Create a Result object to return
        var result = new Result
        {
            Name = this.Name,
            StatusCode = 200,
            Calls = new List<Result>()
        };
        foreach (var task in tasks)
        {
            var response = task.Result;

            Result? subResult = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                subResult = await response.Content.ReadFromJsonAsync<Result>();
            }

            if (subResult != null)
            {
                result.Calls.Add(subResult);
            }
            else
            {
                result.Calls.Add(new Result
                {
                    Name = response.RequestMessage?.RequestUri?.ToString(),
                    StatusCode = (int)response.StatusCode
                });
            }
        }
        return result;
    }
}
