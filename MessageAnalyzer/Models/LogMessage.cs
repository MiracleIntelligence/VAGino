namespace MessageAnalyzer.Models
{
    public class LogMessage : SerialMessage
    {
        public LogMessage(string message)
        {
            _message = message;
        }
    }
}
