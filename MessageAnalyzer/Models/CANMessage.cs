using System.Linq;

namespace MessageAnalyzer.Models
{
    public class CANMessage : SerialMessage
    {
        private int _dlc;
        private string[] _data;

        public CANMessage(string message)
        {
            _message = message;
            Parse(message);
        }

        public string[] Data
        {
            get => _data;
            set => _data = value;
        }
        public int DLC
        {
            get => _dlc;
            set => _dlc = value;
        }

        void Parse(string message)
        {

            var parts = message.Split(' ');
            if (parts.Length > 1)
            {
                _id = parts[0];
                DLC = int.Parse(parts[1]);
                Data = parts.Skip(2).ToArray();
            }
        }
    }
}
