using System.IO.BACnet.EventNotification.EventValues;

namespace System.IO.BACnet.EventNotification
{
    public class StateTransition : EventNotificationData
    {
        private readonly EventNotificationData _eventData;

        #region EventNotificationData properties

        public override uint ProcessIdentifier
        {
            get => _eventData.ProcessIdentifier;
            set => _eventData.ProcessIdentifier = value;
        }

        public override BacnetObjectId InitiatingObjectIdentifier
        {
            get => _eventData.InitiatingObjectIdentifier;
            set => _eventData.InitiatingObjectIdentifier = value;
        }

        public override BacnetObjectId EventObjectIdentifier
        {
            get => _eventData.EventObjectIdentifier;
            set => _eventData.EventObjectIdentifier = value;
        }

        public override BacnetGenericTime TimeStamp
        {
            get => _eventData.TimeStamp;
            set => _eventData.TimeStamp = value;
        }

        public override uint NotificationClass
        {
            get => _eventData.NotificationClass;
            set => _eventData.NotificationClass = value;
        }

        public override byte Priority
        {
            get => _eventData.Priority;
            set => _eventData.Priority = value;
        }

        public override string MessageText
        {
            get => _eventData.MessageText;
            set => _eventData.MessageText = value;
        }

        public override BacnetNotifyTypes NotifyType
        {
            get => _eventData.NotifyType;
            set => _eventData.NotifyType = value;
        }

        public override BacnetEventStates ToState
        {
            get => _eventData.ToState;
            set => _eventData.ToState = value;
        }

        #endregion

        public BacnetEventTypes EventType;
        public bool AckRequired;
        public BacnetEventStates FromState;
        public EventValuesBase EventValues;

        public StateTransition(EventNotificationData eventData)
        {
            _eventData = eventData;
        }
    }
}