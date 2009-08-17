using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClarkKent
{
    [Serializable]
    class InvalidDataException : System.ApplicationException
    {
        private string _message;

        public InvalidDataException() : base() {}

        public InvalidDataException(string message) : base(message) 
        {
            _message = message;
        }

        public InvalidDataException(string message, System.Exception inner) : base(message, inner) 
        {
            _message = message;
        }
 
        // Constructor needed for serialization 
        // when exception propagates from a remoting server to the client.
        protected InvalidDataException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) 
        {
            _message = info.GetString("_message");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("_message", this._message);
            base.GetObjectData(info, context);
        }

        public string getMessage()
        {
            return _message;
        }
    }
}
