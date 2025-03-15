using TestAssessment.ConsoleApp.Models;

namespace TestAssessment.ConsoleApp.Helpers;

public static class ConsoleRenderer
{
    public static void DisplayPeople(IReadOnlyCollection<Person> people)
    {
        Console.WriteLine("\nPeople List:");
        Console.WriteLine("------------------------------------------------");
        foreach (var person in people.Take(20)) // Limit to first 20 results
        {
            Console.WriteLine($"{person.UserName}: {person.FirstName} {person.LastName}");
        }

        Console.WriteLine($"Showing {people.Count} results");
    }

    public static void DisplayPerson(Person person)
    {
        Console.WriteLine("\nPerson Details:");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Username: {person.UserName}");
        Console.WriteLine($"Name: {person.FirstName} {person.LastName} {person.MiddleName}");
        Console.WriteLine("Emails: " + person.Email);
        Console.WriteLine($"Address: {person.Address}");
        Console.WriteLine($"Date of Birth: {person.DateOfBirth.ToString()}");
    }
}