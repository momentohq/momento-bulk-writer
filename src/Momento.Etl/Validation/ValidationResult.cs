namespace Momento.Etl.Validation;

public abstract record ValidationResult
{
    public record OK : ValidationResult
    {
        private static OK _Instance;
        static OK()
        {
            _Instance = new OK();
        }
        public static OK Instance { get => _Instance; }
    }

    public record Error : ValidationResult
    {
        public string Message { get; private set; }

        public Error(string message) => this.Message = message;
    }
}
