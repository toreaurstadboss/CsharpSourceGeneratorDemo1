namespace WiredBrainCoffee.ConsoleApp.Model
{
    public partial class Person
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        internal string? MiddleName { get; set; }

        public bool IsCoolPerson { get; set; }

        //public override string ToString()
        //{
        //    return $"FirstName:{FirstName}; LastName:{LastName}";
        //}
    }
}