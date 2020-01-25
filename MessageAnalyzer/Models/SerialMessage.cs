namespace MessageAnalyzer.Models
{
    public abstract class SerialMessage
    {
        protected string _id;
        protected string _message;

        public string Id => _id;
        public string Message => _message;
        public override string ToString()
        {
            return _message;
        }
    }
}
