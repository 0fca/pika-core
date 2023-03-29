using System;

namespace PikaCore.Infrastructure.Adapters.Filesystem.Exceptions
{
    public class InvalidPathException : Exception
    {
        public InvalidPathException(string message) : base(message) { }
    }
}