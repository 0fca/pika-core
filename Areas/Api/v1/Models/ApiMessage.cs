using System.Collections.Generic;

namespace PikaCore.Areas.Api.v1.Models
{
    public class ApiMessage<T> : IPayload<T>
    {
        private T _data;
        private readonly Stack<string> _messages = new Stack<string>();
        private bool _status = true;
        
        public void SetStatus(bool status)
        {
            this._status = status;
        }

        public bool GetStatus()
        {
            return this._status;
        }

        public void SetData(T o)
        {
            this._data = o;
        }

        public T GetData()
        {
            return this._data;
        }

        public void AddMessage(string message)
        {
            this._messages.Push(message);
        }

        public string GetLastAddedMessage()
        {
            return this._messages.Pop();
        }

        public Stack<string> GetMessages()
        {
            return this._messages;
        }
    }
}