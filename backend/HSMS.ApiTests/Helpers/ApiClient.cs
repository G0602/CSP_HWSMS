using RestSharp;
using System.Diagnostics;

namespace HSMS.ApiTests.Helpers;

/// <summary>
/// Reusable API client for all test cases
/// Encapsulates RestSharp client to avoid code duplication
/// </summary>
public class ApiClient
{
    private readonly RestClient _client;
    private string? _authToken;
    private const int TimeoutMilliseconds = 10000; // 10 seconds

    public ApiClient(string? baseUrl = null)
    {
        baseUrl ??= ApiTestConstants.BaseUrl;
        _client = new RestClient(baseUrl);
    }

    /// <summary>
    /// Sets the authorization token for subsequent requests
    /// </summary>
    public void SetAuthToken(string token)
    {
        _authToken = token;
    }

    /// <summary>
    /// Clears the authorization token
    /// </summary>
    public void ClearAuthToken()
    {
        _authToken = null;
    }

    /// <summary>
    /// Sends a POST request with JSON body (synchronous wrapper)
    /// </summary>
    public RestResponse Post(string endpoint, object body)
    {
        return PostAsync(endpoint, body).Result;
    }

    /// <summary>
    /// Sends a POST request with JSON body (async)
    /// </summary>
    public async Task<RestResponse> PostAsync(string endpoint, object body)
    {
        var request = new RestRequest(endpoint, Method.Post);
        request.AddJsonBody(body);
        request.Timeout = TimeoutMilliseconds;
        
        if (!string.IsNullOrEmpty(_authToken))
        {
            request.AddHeader("Authorization", $"Bearer {_authToken}");
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        stopwatch.Stop();
        
        // Add response time to response object
        response.ErrorException = new ApiResponseTimeException(stopwatch.Elapsed, response.ErrorException);
        
        return response;
    }

    /// <summary>
    /// Sends a GET request (synchronous wrapper)
    /// </summary>
    public RestResponse Get(string endpoint)
    {
        return GetAsync(endpoint).Result;
    }

    /// <summary>
    /// Sends a GET request (async)
    /// </summary>
    public async Task<RestResponse> GetAsync(string endpoint)
    {
        var request = new RestRequest(endpoint, Method.Get);
        request.Timeout = TimeoutMilliseconds;
        
        if (!string.IsNullOrEmpty(_authToken))
        {
            request.AddHeader("Authorization", $"Bearer {_authToken}");
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        stopwatch.Stop();
        
        response.ErrorException = new ApiResponseTimeException(stopwatch.Elapsed, response.ErrorException);
        
        return response;
    }

    /// <summary>
    /// Sends a PUT request with JSON body (synchronous wrapper)
    /// </summary>
    public RestResponse Put(string endpoint, object body)
    {
        return PutAsync(endpoint, body).Result;
    }

    /// <summary>
    /// Sends a PUT request with JSON body (async)
    /// </summary>
    public async Task<RestResponse> PutAsync(string endpoint, object body)
    {
        var request = new RestRequest(endpoint, Method.Put);
        request.AddJsonBody(body);
        request.Timeout = TimeoutMilliseconds;
        
        if (!string.IsNullOrEmpty(_authToken))
        {
            request.AddHeader("Authorization", $"Bearer {_authToken}");
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        stopwatch.Stop();
        
        response.ErrorException = new ApiResponseTimeException(stopwatch.Elapsed, response.ErrorException);
        
        return response;
    }

    /// <summary>
    /// Sends a DELETE request (synchronous wrapper)
    /// </summary>
    public RestResponse Delete(string endpoint)
    {
        return DeleteAsync(endpoint).Result;
    }

    /// <summary>
    /// Sends a DELETE request (async)
    /// </summary>
    public async Task<RestResponse> DeleteAsync(string endpoint)
    {
        var request = new RestRequest(endpoint, Method.Delete);
        request.Timeout = TimeoutMilliseconds;
        
        if (!string.IsNullOrEmpty(_authToken))
        {
            request.AddHeader("Authorization", $"Bearer {_authToken}");
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        stopwatch.Stop();
        
        response.ErrorException = new ApiResponseTimeException(stopwatch.Elapsed, response.ErrorException);
        
        return response;
    }

    /// <summary>
    /// Extension method to get response time from exception
    /// </summary>
    public static TimeSpan? GetResponseTime(RestResponse response)
    {
        if (response.ErrorException is ApiResponseTimeException apiException)
        {
            return apiException.ResponseTime;
        }
        return null;
    }
}

/// <summary>
/// Custom exception to carry response time information
/// </summary>
internal class ApiResponseTimeException : Exception
{
    public TimeSpan ResponseTime { get; }

    public ApiResponseTimeException(TimeSpan responseTime, Exception? innerException = null)
        : base($"API request took {responseTime.TotalMilliseconds}ms", innerException)
    {
        ResponseTime = responseTime;
    }
}
