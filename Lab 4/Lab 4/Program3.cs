using System;
using System.Collections.Generic;
using System.Threading;

namespace RetryMechanismWithExponentialDelayAndRandomization
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
        private readonly Dictionary<string, List<Action<EventData>>> _eventHandlers;
        private readonly object _lockObject = new object();

        private readonly int _throttleLimit;
        private readonly TimeSpan _throttleInterval;
        private DateTime _lastDispatchTime;

        public EventBus(int throttleLimit, TimeSpan throttleInterval)
        {
            _eventHandlers = new Dictionary<string, List<Action<EventData>>>();
            _throttleLimit = throttleLimit;
            _throttleInterval = throttleInterval;
            _lastDispatchTime = DateTime.MinValue;
        }

        public void RegisterHandler(string eventName, Action<EventData> eventHandler)
        {
            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName] = new List<Action<EventData>>();
                }

                _eventHandlers[eventName].Add(eventHandler);
            }
        }

        public void UnregisterHandler(string eventName, Action<EventData> eventHandler)
        {
            lock (_lockObject)
            {
                if (_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName].Remove(eventHandler);
                }
            }
        }

        public void Dispatch(EventData eventData, Func<bool> retryCondition, Func<int, TimeSpan> retryDelay)
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
                        if (!retryCondition())
                        {
                            return;
                        }

                        var retryCount = 0;

                        while (true)
                        {
                            try
                            {
                                handler(eventData);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error dispatching event '{eventData.EventName}' to handler '{handler.Method.Name}' on attempt #{retryCount + 1}: {ex.Message}");

                                if (!retryCondition())
                                {
                                    return;
                                }

                                var delay = retryDelay(retryCount);
                                Console.WriteLine($"Retrying event '{eventData.EventName}' to handler '{handler.Method.Name}' in {delay.TotalMilliseconds}ms");
                                Thread.Sleep(delay);

                                retryCount++;
                            }
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

        public void Publish(string eventName, object eventPayload, EventPriority eventPriority, int retryCount, TimeSpan initialDelay, TimeSpan maxDelay)
        {
            var eventData = new EventData { EventName = eventName, EventPayload = eventPayload, EventPriority = eventPriority };
            var retryPolicy = CreateRetryPolicy(retryCount, initialDelay, maxDelay);
            _eventBus.Dispatch(eventData, retryPolicy.Retry
