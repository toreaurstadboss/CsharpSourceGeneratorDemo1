using WiredBrainCoffee.Generators;

namespace WiredBrainCoffee.ConsoleApp.Model;

[GenerateToString]
public partial class Person
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}