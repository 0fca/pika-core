using System;

namespace FMS.Exceptions
{
    public class InvalidPathException : Exception
    {
        public InvalidPathException(string message) : base(message) { }
    }
}