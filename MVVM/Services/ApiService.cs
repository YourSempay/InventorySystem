using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MVVM.Services;

public class ApiService
{
    private readonly HttpClient client;
    private readonly AuthService auth;

    public ApiService(AuthService auth)
    {
        this.auth = auth;

        client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5090/api/")
        };
    }

    private void AddAuthorizationHeader()
    {
        client.DefaultRequestHeaders.Authorization = null;

        if (auth.IsAuthenticated && !string.IsNullOrEmpty(auth.Token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", auth.Token);
        }
    }

    private void CheckTokenExpiration()
    {
        if (!auth.IsAuthenticated)
            throw new UnauthorizedAccessException("");
    }

    private async Task HandleResponseErrors(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            auth.Logout();
            throw new UnauthorizedAccessException("");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Error: {errorContent}");
        }
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        var response = await client.GetAsync(url);
        await HandleResponseErrors(response);

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var jsonString = await reader.ReadToEndAsync();

        return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        AddAuthorizationHeader();

        var response = await client.PostAsJsonAsync(url, data);
        await HandleResponseErrors(response);

        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task PostAsync<TRequest>(string url, TRequest data)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        var response = await client.PostAsJsonAsync(url, data);
        await HandleResponseErrors(response);
    }

    public async Task PutAsync<TRequest>(string url, TRequest data)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        var response = await client.PutAsJsonAsync(url, data);
        await HandleResponseErrors(response);
    }
    
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        HttpResponseMessage response;

        if (data is MultipartFormDataContent multipart)
        {
            response = await client.PutAsync(url, multipart);
        }
        else
        {
            response = await client.PutAsJsonAsync(url, data);
        }

        await HandleResponseErrors(response);
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task DeleteAsync(string url)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        var response = await client.DeleteAsync(url);
        await HandleResponseErrors(response);
    }

    public async Task PostMultipartAsync(string url, MultipartFormDataContent content)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        var response = await client.PostAsync(url, content);
        await HandleResponseErrors(response);
    }

    public async Task<TResponse?> PostMultipartAsync<TResponse>(string url, MultipartFormDataContent content)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        var response = await client.PostAsync(url, content);
        await HandleResponseErrors(response);

        return await response.Content.ReadFromJsonAsync<TResponse>();
    }
    
    public async Task<byte[]?> PostForFileAsync<TRequest>(string url, TRequest data)
    {
        CheckTokenExpiration();
        AddAuthorizationHeader();

        var response = await client.PostAsJsonAsync(url, data);
        await HandleResponseErrors(response);

        return await response.Content.ReadAsByteArrayAsync();
    }

}