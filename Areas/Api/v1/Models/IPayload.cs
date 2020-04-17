using System;
using System.Collections.Generic;

namespace PikaCore.Areas.Api.v1.Models
{
    public interface IPayload<T>
    {
        void SetStatus(bool status);
        bool GetStatus();
        void SetData(T o);
        T GetData();
        void AddMessage(string message);
        string GetLastAddedMessage();
        Stack<string> GetMessages();
    }
}