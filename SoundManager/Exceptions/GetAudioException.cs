using System;

namespace Exceptions
{
    public class GetAudioException : Exception
    {
        public GetAudioException()
        {
        }

        public GetAudioException(string message)
            : base(message)
        {
        }

        public GetAudioException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}