using System;
using MessageAnalyzer.Models;

namespace MessageAnalyzer
{
    public class Analyzer
    {
        private string _msgSeparator = Config.DEFAULT_MESSAGE_SEPARATOR;

        public SerialMessage Analyze(string rawMessage)
        {
            var elements = rawMessage.Split(new string[] { _msgSeparator }, StringSplitOptions.None);
            return GetMessage(elements);
        }

        public Analyzer SetCmdSeparator(string separator)
        {
            _msgSeparator = separator;
            return this;
        }

        private SerialMessage GetMessage(string[] elements)
        {
            if (elements.Length == 2)
            {
                SerialMessage serialMessage = null;
                var cmdType = (SerialMsgType)Enum.Parse(typeof(SerialMsgType), elements[0]);
                switch (cmdType)
                {
                    case SerialMsgType.CAN: serialMessage = GetCanMessage(elements[1]); break;
                    case SerialMsgType.DBG: serialMessage = GetLogMessage(elements[1]); break;
                }

                return serialMessage;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(elements));
            }
        }

        private SerialMessage GetLogMessage(string message)
        {
            return new LogMessage(message);
        }

        private SerialMessage GetCanMessage(string message)
        {
            var msg = new CANMessage(message);
            return msg;
        }
    }
}
