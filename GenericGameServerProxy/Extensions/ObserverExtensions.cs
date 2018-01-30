using System;

namespace GenericGameServerProxy
{
    public static class ObserverExtensions
    {
        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }
    }
}
