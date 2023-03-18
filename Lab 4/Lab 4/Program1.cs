using System;
using System.Collections.Generic;
using System.Threading;

namespace CustomEventBusWithThrottling
{
    public class EventData
    {
        public string EventName { get; set; }
        public object EventPayload { get; set; }
    }

    public class EventBus
    {
        private readonly Dictionary<string, List<Delegate>> _eventHandlers;
        private readonly object _lockObject = new object();

        // Throttling properties
        private readonly int _throttleLimit;
        private readonly TimeSpan _throttleInterval;
        private DateTime _lastDispatchTime;

        public EventBus(int throttleLimit, TimeSpan throttleInterval)
        {
            _eventHandlers = new Dictionary<string, List<Delegate>>();
            _throttleLimit = throttleLimit;
            _throttleInterval = throttleInterval;
            _lastDispatchTime = DateTime.MinValue;
        }

        public void RegisterHandler(string eventName, Delegate eventHandler)
        {
            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName] = new List<Delegate>();
                }

                _eventHandlers[eventName].Add(eventHandler);
            }
        }

        public void UnregisterHandler(string eventName, Delegate eventHandler)
        {
            lock (_lockObject)
            {
                if (_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName].Remove(eventHandler);
                }
            }
        }

        public void Dispatch(string eventName, object eventPayload)
        {
            lock (_lockObject)
            {
                // Throttling check
                var timeSinceLastDispatch = DateTime.Now - _lastDispatchTime;
                if (timeSinceLastDispatch < _throttleInterval)
                {
                    Thread.Sleep(_throttleInterval - timeSinceLastDispatch);
                }

                // Dispatch event
                if (_eventHandlers.ContainsKey(eventName))
                {
                    var eventData = new EventData { EventName = eventName, EventPayload = eventPayload };
                    foreach (var handler in _eventHandlers[eventName])
                    {
                        try
                        {
                            handler.DynamicInvoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error dispatching event '{eventName}' to handler '{handler.Method.Name}': {ex.Message}");
                        }
                    }
                }

                _lastDispatchTime = DateTime.Now;
            }
        }
    }

    public class ExampleEventHandler
    {
        public void HandleEvent(EventData eventData)
        {
            Console.WriteLine($"Handling event '{eventData.EventName}' with payload '{eventData.EventPayload}'");
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var eventBus = new EventBus(throttleLimit: 2, throttleInterval: TimeSpan.FromSeconds(1));
            var eventHandler = new ExampleEventHandler();
            eventBus.RegisterHandler("example_event", new Action<EventData>(eventHandler.HandleEvent));

            for (int i = 0; i < 10; i++)
            {
                eventBus.Dispatch("example_event", i);
            }

            Console.ReadKey();
        }
    }
}