using System;
using System.Collections.Generic;
using System.Text;

namespace RecognitionLibrary
{
    public delegate void UserMessageEventHandler(object sender, UserMessageHandlerEventArgs args);

    public class UserMessageHandlerEventArgs : EventArgs
    {
        public string Message { get; set; }

        public UserMessageHandlerEventArgs(string message)
        {
            this.Message = message;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
