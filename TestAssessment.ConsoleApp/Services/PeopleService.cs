using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestAssessment.ConsoleApp.Models;

namespace TestAssessment.ConsoleApp.Services;

public class PeopleService : IPeopleService
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions;

    public PeopleService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        jsonOptions = new() { PropertyNameCaseInsensitive = true };
    }

    public async Task<IReadOnlyCollection<Person>> SearchPeopleAsync(string? searchTerm)
    {
        var filter = BuildSearchQuery(searchTerm);
        if (string.IsNullOrEmpty(filter))
        {
            return await GetAllPeopleAsync();
        }

        var encodedFilter = Uri.EscapeDataString(filter);
        var response = await httpClient.GetAsync($"People?$filter={encodedFilter}");

        response.EnsureSuccessStatusCode();
        var result = await JsonSerializer.DeserializeAsync<ApiResponse<Person>>(
            await response.Content.ReadAsStreamAsync(), jsonOptions);
        return result?.Value ?? [];
    }

    public async Task<Person?> FindPersonDetailsAsync(string userName)
    {
        var response = await httpClient.GetAsync($"People('{userName}')");
        return response.IsSuccessStatusCode
            ? await JsonSerializer.DeserializeAsync<Person?>(
                await response.Content.ReadAsStreamAsync(), jsonOptions)
            : null;
    }

    public async Task<bool> UpdatePersonAsync(Person person)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(person), Encoding.UTF8, "application/json");
        var response = await httpClient.PatchAsync($"People('{person.UserName}')", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePersonAsync(string userName)
    {
        var response = await httpClient.DeleteAsync($"People('{userName}')");
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyCollection<Person>> GetAllPeopleAsync()
    {
        var response = await httpClient.GetAsync("People");
        response.EnsureSuccessStatusCode();
        var result = await JsonSerializer.DeserializeAsync<ApiResponse<Person>>(
            await response.Content.ReadAsStreamAsync(), jsonOptions);
        return result?.Value ?? [];
    }

    private static string BuildSearchQuery(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return string.Empty;
        }

        return $"contains(tolower(FirstName), tolower('{searchTerm}')) " +
               $"or contains(tolower(LastName), tolower('{searchTerm}'))";
    }

    private record ApiResponse<T>
    {
        [JsonPropertyName("value")]
        public required List<T> Value { get; init; }
    }
}