namespace WebDog.Models
{
    public class EnvVariable
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N")[..8];
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Enabled { get; set; } = true;
    }
}
