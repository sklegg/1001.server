namespace Server1001.Shared
{
    public class ErrorModel
    {
        public string ErrorId { get; init; } = Guid.NewGuid().ToString();
        public string ErrorMessage { get; init; }

        public int? StatusCode { get; init; }

        public ErrorModel (string message, int? code)
        {
            ErrorMessage = message;
            StatusCode = code;
        }
    }
}