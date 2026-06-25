using System;
using UnityEngine;

namespace MP.Core.Events
{
    /*
        ScriptableObject 기반의 공용 이벤트 채널로, 서로 직접 참조하지 않는 시스템 간에 이벤트를 전달한다.
        같은 이벤트를 주고받는 객체들은 반드시 동일한 EventChannel 에셋을 참조해야 한다.

        ScriptableObject는 씬 오브젝트보다 오래 살아남을 수 있으므로 씬 전환 후에도 리스너가 남을 수 있다.
        따라서 Register한 리스너는 OnDisable 등에서 반드시 Unregister해야 한다.
    */
    public abstract class EventChannel<TEvent> : ScriptableObject
    {
        // 이 이벤트를 구독한 함수 목록
        private event Action<TEvent> Raised;

        // 이벤트 구독 (중복 구독 방지)
        public void Register(Action<TEvent> listener)
        {
            if (listener == null)   return;

            Raised -= listener;
            Raised += listener;
        }

        // 이벤트 구독 해제
        public void Unregister(Action<TEvent> listener)
        {
            if (listener == null)   return;

            Raised -= listener;
        }

        // 이벤트가 발생함을 알림
        // ※ public이므로 구독자가 이벤트를 발생시킬 수 있으므로 발신자와 수신자의 경계가 모호해질 수 있음
        // ※ IEventReader, IEventWriter 인터페이스를 사용해 그 경계를 명확히 할 수 있음
        public void Raise(TEvent eventData)
        {
            if (Raised == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Fail-Fast 구조
            // 어떤 하나의 리스너가 예외를 던지면 그 뒤에 등록되어 있는 리스너들은 이벤트를 받지 못함
            // 개발 중에는 바로 터뜨려서 버그를 빨리 찾는다
            Raised.Invoke(eventData);
#else
            // Try-Catch 구조
            // 어떤 하나의 리스너가 예외를 던져도 로깅만 하고 넘어감
            // 실제 빌드에서는 하나의 리스너 오류가 전체 이벤트 전파를 막지 않게 한다
            foreach (Action<TEvent> listener in Raised.GetInvocationList())
            {
                try
                {
                    listener.Invoke(eventData);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
#endif
        }

        // 이벤트를 구독한 함수 목록을 모두 비움 (암시적)
        protected virtual void OnDisable()
        {
            ClearListeners();
        }

        // 이벤트를 구독한 함수 목록을 모두 비움 (명시적)
        protected void ClearListeners()
        {
            Raised = null;
        }
    }
}
