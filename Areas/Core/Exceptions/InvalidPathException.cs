using System;

namespace PikaCore.Areas.Core.Exceptions
{
    public class InvalidPathException : Exception
    {
        public InvalidPathException(string message) : base(message) { }
    }
}