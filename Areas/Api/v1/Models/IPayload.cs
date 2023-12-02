using System.Collections.Generic;

namespace PikaCore.Areas.Api.v1.Models;

public interface IPayload<T>
{
    void AddMessage(string message);
    string GetLastAddedMessage();
    Stack<string> GetMessages();
}