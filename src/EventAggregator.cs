using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventAggregator.Blazor
{
    public class EventAggregator : IEventAggregator
    {
        private readonly EventAggregatorOptions _options;
        private readonly List<Handler> _handlers = new List<Handler>();

        public EventAggregator(IOptions<EventAggregatorOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc />
        public virtual void Subscribe(object subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            lock (_handlers)
            {
                if (_handlers.Any(x => x.Matches(subscriber)))
                {
                    return;
                }

                _handlers.Add(new Handler(subscriber));
            }
        }

        /// <inheritdoc />
        public virtual void Unsubscribe(object subscriber)
        {
            if (subscriber == null)
            {
                throw new ArgumentNullException(nameof(subscriber));
            }

            lock (_handlers)
            {
                var handlersFound = _handlers.FirstOrDefault(x => x.Matches(subscriber));

                if (handlersFound != null)
                {
                    _handlers.Remove(handlersFound);
                }
            }
        }

        public virtual async Task PublishAsync(object message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Handler[] handlersToNotify;

            lock (_handlers)
            {
                handlersToNotify = _handlers.ToArray();
            }

            var messageType = message.GetType();

            var tasks = handlersToNotify.Select(h => h.Handle(messageType, message));

            await Task.WhenAll(tasks);

            if (_options.AutoRefresh)
            {
                foreach (var handler in handlersToNotify.Where(x => !x.IsDead))
                {
                    if (!(handler.Reference.Target is ComponentBase component))
                    {
                        continue;
                    }

                    var invoker = component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                        .FirstOrDefault(x =>
                            string.Equals(x.Name, "InvokeAsync") &&
                            x.GetParameters().FirstOrDefault()?.ParameterType == typeof(Action));

                    if (invoker == null)
                    {
                        continue;
                    }
                    
                    var stateHasChangedMethod = component.GetType()
                        .GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (stateHasChangedMethod == null)
                    {
                        continue;
                    }
                    
                    var args = new object[] { new Action(() => stateHasChangedMethod.Invoke(component, null)) };
                    var tOut = (Task) invoker.Invoke(component, args);
                            
                    await tOut;
                }
            }

            var deadHandlers = handlersToNotify.Where(h => h.IsDead).ToList();

            if (deadHandlers.Any())
            {
                lock (_handlers)
                {
                    foreach (var item in deadHandlers)
                    {
                        _handlers.Remove(item);
                    }
                }
            }
        }

        private class Handler
        {
            private readonly WeakReference _reference;
            private readonly Dictionary<Type, MethodInfo> _supportedHandlers = new Dictionary<Type, MethodInfo>();

            public Handler(object handler)
            {
                _reference = new WeakReference(handler);

                var interfaces = handler.GetType().GetTypeInfo().ImplementedInterfaces
                    .Where(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandle<>));

                foreach (var handleInterface in interfaces)
                {
                    var type = handleInterface.GetTypeInfo().GenericTypeArguments[0];
                    var method = handleInterface.GetRuntimeMethod("HandleAsync", new[] {type});

                    if (method != null)
                    {
                        _supportedHandlers[type] = method;
                    }
                }
            }

            public bool IsDead => _reference.Target == null;

            public WeakReference Reference => _reference;

            public bool Matches(object instance)
            {
                return _reference.Target == instance;
            }

            public Task Handle(Type messageType, object message)
            {
                var target = _reference.Target;

                if (target == null)
                {
                    return Task.FromResult(false);
                }

                var tasks = _supportedHandlers
                    .Where(handler => handler.Key.GetTypeInfo().IsAssignableFrom(messageType.GetTypeInfo()))
                    .Select(pair => pair.Value.Invoke(target, new[] {message}))
                    .Select(result => (Task) result)
                    .ToList();

                return Task.WhenAll(tasks);
            }

            public bool Handles(Type messageType)
            {
                return _supportedHandlers.Any(
                    pair => pair.Key.GetTypeInfo().IsAssignableFrom(messageType.GetTypeInfo()));
            }
        }
    }
}