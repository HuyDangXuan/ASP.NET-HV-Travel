using System;

namespace HVTravel.Infrastructure.Data
{
    public class DbUpdateConcurrencyException : Exception
    {
        public DbUpdateConcurrencyException(string message) : base(message)
        {
        }
    }
}
