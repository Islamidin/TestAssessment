using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestAssessment.ConsoleApp.Helpers;
using TestAssessment.ConsoleApp.Services;

var host = Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((_, config) => { config.AddJsonFile("appsettings.json", false, true); })
               .ConfigureServices((context, services) => { services.AddHttpClient<IPeopleService, PeopleService>(client => { client.BaseAddress = new(context.Configuration["Api:BaseUrl"]!); }); })
               .Build();

var peopleService = host.Services.GetRequiredService<IPeopleService>();

while (true)
{
    Console.Clear();
    DisplayMenu();

    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    try
    {
        switch (input)
        {
            case "1":
                await ListPeople(peopleService);
                break;
            case "2":
                await SearchPeople(peopleService);
                break;
            case "3":
                await ViewPersonDetails(peopleService);
                break;
            case "4":
                await UpdatePerson(peopleService);
                break;
            case "5":
                await DeletePerson(peopleService);
                break;
            case "6":
                return;
            default:
                Console.WriteLine("Invalid option. Please choose from the list:");
                DisplayMenu();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey();
}

static void DisplayMenu()
{
    Console.WriteLine("1. List People");
    Console.WriteLine("2. Search People");
    Console.WriteLine("3. View Person Details");
    Console.WriteLine("4. Update Person");
    Console.WriteLine("5. Delete Person");
    Console.WriteLine("6. Exit");
    Console.Write("Choose an option: ");
}

static async Task ListPeople(IPeopleService service)
{
    var people = await service.GetAllPeopleAsync();
    ConsoleRenderer.DisplayPeople(people);
}

static async Task SearchPeople(IPeopleService service)
{
    Console.Write("Enter search term: ");
    var term = Console.ReadLine();
    var people = await service.SearchPeopleAsync(term);
    ConsoleRenderer.DisplayPeople(people);
}

static async Task ViewPersonDetails(IPeopleService service)
{
    Console.Write("Enter username: ");
    var userName = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userName))
    {
        return;
    }

    var person = await service.FindPersonDetailsAsync(userName);
    if (person != null)
    {
        ConsoleRenderer.DisplayPerson(person);
    }
    else
    {
        Console.WriteLine("Person not found!");
    }
}

static async Task UpdatePerson(IPeopleService service)
{
    Console.Write("Enter username to update: ");
    var userName = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userName))
    {
        return;
    }

    var person = await service.FindPersonDetailsAsync(userName);
    if (person == null)
    {
        Console.WriteLine("Person not found!");
        return;
    }

    Console.WriteLine($"Editing: {person.FirstName} {person.LastName}");
    Console.Write("New first name (leave empty to keep current): ");
    var firstName = Console.ReadLine();
    Console.Write("New last name (leave empty to keep current): ");
    var lastName = Console.ReadLine();

    var newPerson = person with { FirstName = firstName ?? person.FirstName, LastName = lastName ?? person.LastName };

    var result = await service.UpdatePersonAsync(newPerson);
    Console.WriteLine(result ? "Person updated successfully!" : "Person was not updated!");
}

static async Task DeletePerson(IPeopleService service)
{
    Console.Write("Enter username to delete: ");
    var userName = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userName))
    {
        return;
    }

    var person = await service.FindPersonDetailsAsync(userName);
    if (person == null)
    {
        Console.WriteLine("Person not found!");
        return;
    }

    Console.WriteLine($"Deleting: {person.FirstName} {person.LastName}");

    var result = await service.DeletePersonAsync(userName);
    Console.WriteLine(result ? "Person deleted successfully!" : "Person was not deleted!");
}