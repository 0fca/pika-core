using System.Collections.Generic;

namespace PikaCore.Areas.Api.v1.Models;

public class ApiMessage<T> : IPayload<T>
{
    public T Data { get; set; }
    public Stack<string> Messages { get; set; } = new();
    public bool Status { get; set; } = true;

    public void AddMessage(string message)
    {
        Messages.Push(message);
    }

    public string GetLastAddedMessage()
    {
        return Messages.Pop();
    }

    public Stack<string> GetMessages()
    {
        return Messages;
    }
}