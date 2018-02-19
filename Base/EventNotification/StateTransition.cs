namespace System.IO.BACnet.EventNotification
{
    public class StateTransition : EventNotificationData
    {
        public BacnetEventTypes EventType;
        public bool AckRequired;
        public BacnetEventStates FromState;

        public StateTransition(EventNotificationData eventData)
        {
            ProcessIdentifier = eventData.ProcessIdentifier;
            InitiatingObjectIdentifier = eventData.InitiatingObjectIdentifier;
            EventObjectIdentifier = eventData.EventObjectIdentifier;
            TimeStamp = eventData.TimeStamp;
            NotificationClass = eventData.NotificationClass;
            Priority = eventData.Priority;
            MessageText = eventData.MessageText;
            NotifyType = eventData.NotifyType;
            ToState = eventData.ToState;
        }

        public StateTransition(StateTransition transition) : this(transition as EventNotificationData)
        {
            EventType = transition.EventType;
            AckRequired = transition.AckRequired;
            FromState = transition.FromState;
        }
    }
}