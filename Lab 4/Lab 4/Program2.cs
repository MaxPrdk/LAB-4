using System;
using System.Collections.Generic;
using System.Threading;

namespace PrioritizedPublishSubscribePattern
{
    public enum EventPriority
    {
        High,
        Medium,
        Low
    }

    public class EventData
    {
        public string EventName { get; set; }
        public object EventPayload { get; set; }
        public EventPriority EventPriority { get; set; }
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

        public void Dispatch(EventData eventData)
        {
            lock (_lockObject)
            {
                
                var timeSinceLastDispatch = DateTime.Now - _lastDispatchTime;
                if (timeSinceLastDispatch < _throttleInterval)
                {
                    Thread.Sleep(_throttleInterval - timeSinceLastDispatch);
                }

              
                if (_eventHandlers.ContainsKey(eventData.EventName))
                {
                    foreach (var handler in _eventHandlers[eventData.EventName])
                    {
                        try
                        {
                            handler.DynamicInvoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error dispatching event '{eventData.EventName}' to handler '{handler.Method.Name}': {ex.Message}");
                        }
                    }
                }

                _lastDispatchTime = DateTime.Now;
            }
        }
    }

    public class Publisher
    {
        private readonly EventBus _eventBus;

        public Publisher(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Publish(string eventName, object eventPayload, EventPriority eventPriority)
        {
            var eventData = new EventData { EventName = eventName, EventPayload = eventPayload, EventPriority = eventPriority };
            _eventBus.Dispatch(eventData);
        }
    }

    public class HighPrioritySubscriber
    {
        public void HandleHighPriorityEvent(EventData eventData)
        {
            if (eventData.EventPriority == EventPriority.High)
            {
                Console.WriteLine($"Handling high priority event '{eventData.EventName}' with payload '{eventData.EventPayload}'");
            }
        }
    }

    public class MediumPrioritySubscriber
    {
        public void HandleMediumPriorityEvent(EventData eventData)
        {
            if (eventData.EventPriority == EventPriority.Medium)
            {
                Console.WriteLine($"Handling medium priority event '{eventData.EventName}' with payload '{eventData.EventPayload}'");
            }
        }
    }

    public class LowPrioritySubscriber
    {
        public void HandleLowPriorityEvent(EventData eventData)
        {
            if (eventData.EventPriority
