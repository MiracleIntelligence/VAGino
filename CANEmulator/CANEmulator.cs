using System.Collections.Generic;
using System.Diagnostics;

namespace CANEmulator
{
    public class CANEmulator
    {
        Stack<string> _canBus;

        object _lockObject;
        public CANEmulator()
        {
            _canBus = new Stack<string>();
            _lockObject = new object();


            _canBus.Push("CAN:0AE 8 E1 E0 00 00 00 44 01 00;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 11 00 01 00;");
            _canBus.Push("LOG:Test log;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 03 00 01 00;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 00 20 01 00;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 00 00 01 00;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 03 00 01 00;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 03 10 01 00;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 03 20 01 00;");
            _canBus.Push("CAN:0AE 8 E1 E0 00 00 03 00 01 00;");
        }
        public void SendMessage(string message)
        {
            Debug.WriteLine(message);
            lock (_lockObject)
            {
                _canBus.Push(message);
            }
        }

        public IEnumerable<string> GetMessage()
        {
            string msg = null;
            lock (_lockObject)
            {
                if (_canBus.Count > 0)
                {
                    msg = _canBus.Pop();
                }
            }

            yield return msg;
        }
    }
}
