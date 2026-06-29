using WebDog.ViewModels;

namespace WebDog.Models
{
    public class KeyValuePairModel : ViewModelBase
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N")[..8];

        private string _key = "";
        public string Key { get => _key; set => SetProperty(ref _key, value); }

        private string _value = "";
        public string Value { get => _value; set => SetProperty(ref _value, value); }

        private string _description = "";
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private bool _enabled = true;
        public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
    }
}
