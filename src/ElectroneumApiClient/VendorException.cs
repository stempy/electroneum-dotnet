using System;

namespace ElectroneumApiClient
{
    public class VendorException : Exception
    {
        public VendorException() { }

        public VendorException(string message):base(message)
        {
        }

        public VendorException(string message, Exception innerException) : base(message,innerException)
        {
        }

    }
}
