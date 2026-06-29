namespace WebDog.Models
{
    public class FormParamModel : KeyValuePairModel
    {
        private string _paramType = "text";
        public string ParamType { get => _paramType; set => SetProperty(ref _paramType, value); }

        private string _fileName = "";
        public string FileName { get => _fileName; set => SetProperty(ref _fileName, value); }

        private long _fileSize;
        public long FileSize { get => _fileSize; set => SetProperty(ref _fileSize, value); }
    }
}
