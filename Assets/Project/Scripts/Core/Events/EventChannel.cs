using System;
using UnityEngine;

namespace MP.Core.Events
{
    public abstract class EventChannel<TEvent> : ScriptableObject
    {
        private event Action<TEvent> Raised;

        public void Raise(TEvent eventData)
        {
            Raised?.Invoke(eventData);
        }

        public void Register(Action<TEvent> listener)
        {
            if (listener == null)
            {
                return;
            }

            Raised -= listener;
            Raised += listener;
        }

        public void Unregister(Action<TEvent> listener)
        {
            if (listener == null)
            {
                return;
            }

            Raised -= listener;
        }
    }
}
