namespace MessageAnalyzer.Models
{
    public class CommandMessage : SerialMessage
    {
        public CommandMessage(string id, string message)
        {
            _id = id;
            _message = message;
        }
    }
}
