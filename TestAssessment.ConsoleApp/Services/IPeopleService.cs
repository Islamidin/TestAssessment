using TestAssessment.ConsoleApp.Models;

namespace TestAssessment.ConsoleApp.Services;

public interface IPeopleService
{
    Task<IReadOnlyCollection<Person>> GetAllPeopleAsync();

    Task<IReadOnlyCollection<Person>> SearchPeopleAsync(string? searchTerm);

    Task<Person?> FindPersonDetailsAsync(string userName);

    Task<bool> UpdatePersonAsync(Person person);

    Task<bool> DeletePersonAsync(string userName);
}