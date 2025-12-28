using System.Collections.Specialized;

namespace Tests.Models
{
    public class EventListenerData
    {
        public EventListenerData() { }

        public EventListenerData(object? sender, NotifyCollectionChangedEventArgs? args)
        {
            Sender = sender;
            Args = args;
        }

        public object? Sender { get; set; } = null;

        public NotifyCollectionChangedEventArgs? Args { get; set; } = null;
    }
}
