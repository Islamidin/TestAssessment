using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using TestAssessment.ConsoleApp.Models;
using TestAssessment.ConsoleApp.Services;
using Xunit;

namespace TestAssessment.Tests.Services;

public class TestPeopleService
{
    private readonly JsonSerializerOptions jsonOptions;
    private readonly Mock<HttpMessageHandler> mockHandler;
    private readonly PeopleService service;

    public TestPeopleService()
    {
        mockHandler = new();
        HttpClient httpClient = new(mockHandler.Object)
        {
            BaseAddress = new("http://test.com/")
        };
        service = new(httpClient);
        jsonOptions = new() { PropertyNameCaseInsensitive = true };
    }

    [Fact]
    public async Task GetAllPeopleAsync_ReturnsPeople()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage
        {
            Content = new StringContent("{ \"value\": [ { \"UserName\": \"testuser\" } ] }")
        };

        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(mockResponse);

        // Act
        var result = await service.GetAllPeopleAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("testuser", result.First().UserName);
    }

    [Fact]
    public async Task GetAllPeopleAsync_ReturnsPeople_WhenSuccessful()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage
        {
            Content = new StringContent(
                """
                { "value": [ 
                                        { "UserName": "user1", "FirstName": "John", "LastName": "Doe" },
                                        { "UserName": "user2", "FirstName": "Jane", "LastName": "Smith" }
                                    ]}
                """, Encoding.UTF8, "application/json")
        };

        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(mockResponse);

        // Act
        var result = await service.GetAllPeopleAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.UserName == "user1");
        Assert.Contains(result, p => p.UserName == "user2");
    }

    [Fact]
    public async Task SearchPeopleAsync_AppliesCorrectFilter()
    {
        // Arrange
        const string searchTerm = "test";
        const string expectedFilter = "contains(tolower(FirstName), tolower('test')) " +
                                      "or contains(tolower(LastName), tolower('test'))";
        var encodedFilter = Uri.EscapeDataString(expectedFilter);
        var expectedQuery = $"?$filter={encodedFilter}";

        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                """{ "value": [] }""",
                Encoding.UTF8,
                "application/json")
        };

        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(mockResponse);

        // Act
        var result = await service.SearchPeopleAsync(searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        // Verify the request was made with correct query
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                                              req.RequestUri!.Query.Contains(expectedQuery)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task FindPersonDetailsAsync_ReturnsPerson_WhenExists()
    {
        // Arrange
        const string userName = "user1";
        var mockPerson = MakePerson(userName);

        var mockResponse = new HttpResponseMessage
        {
            Content = new StringContent(
                JsonSerializer.Serialize(mockPerson, jsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(mockResponse);

        // Act
        var result = await service.FindPersonDetailsAsync(userName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userName, result.UserName);
    }

    [Fact]
    public async Task FindPersonDetailsAsync_ReturnsNull_NotExists()
    {
        // Arrange
        const string userName = "user1";

        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        };

        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(mockResponse);

        // Act
        var result = await service.FindPersonDetailsAsync(userName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePersonAsync_SendsPatchRequest_WithCorrectContent()
    {
        // Arrange
        const string userName = "user1";
        const string expectedUrl = $"People('{userName}')";
        var person = MakePerson(userName, "Updated");
        var expectedContent = JsonSerializer.Serialize(person);

        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        // Act
        await service.UpdatePersonAsync(person);

        // Assert
        mockHandler.Protected().Verify("SendAsync",
                                       Times.Once(),
                                       ItExpr.Is<HttpRequestMessage>(req =>
                                                                         req.Method == HttpMethod.Patch &&
                                                                         req.RequestUri != null &&
                                                                         req.RequestUri.ToString().Contains(expectedUrl) &&
                                                                         req.Content != null &&
                                                                         VerifyContent(req.Content, expectedContent)),
                                       ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAllPeopleAsync_ThrowsException_OnErrorResponse()
    {
        // Arrange
        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetAllPeopleAsync());
    }

    private bool VerifyContent(HttpContent content, string expectedContent)
    {
        var json = content.ReadAsStringAsync().Result;
        return JsonSerializer.Serialize(JsonSerializer.Deserialize<Person>(json), jsonOptions)
               == expectedContent;
    }

    private static Person MakePerson(string userName, string firstName = "first name") => new(userName, firstName, "last name", null, "test@test.com", "address", null);
}