namespace VcogBookmark.Shared.Models
{
    public class Person
    {
        public Person(string login, string password, string role)
        {
            Login = login;
            Password = password;
            Role = role;
        }

        public string Login { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}