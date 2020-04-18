using System.Collections.Generic;

namespace PikaCore.Areas.Api.v1.Models
{
    public class ApiMessage<T> : IPayload<T>
    {
        public T Data { get; set; }
        public Stack<string> Messages { get; set; } = new Stack<string>();
        public bool Status { get; set; } = true;

        public void AddMessage(string message)
        {
            this.Messages.Push(message);
        }

        public string GetLastAddedMessage()
        {
            return this.Messages.Pop();
        }

        public Stack<string> GetMessages()
        {
            return this.Messages;
        }
    }
}