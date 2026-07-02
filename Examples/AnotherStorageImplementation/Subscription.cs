/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mkKvistgaard@pch-engineering.dk>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.BACnet;
using BaCSharp;

namespace AnotherStorageImplementation
{
    public class Subscription
    {
        public BacnetClient reciever;
        public BacnetAddress reciever_address;
        public uint subscriberProcessIdentifier;
        public BacnetObjectId monitoredObjectIdentifier;
        public BacnetPropertyReference monitoredProperty;
        public bool issueConfirmedNotifications;
        public uint lifetime;
        public DateTime start;
        public float covIncrement;
        public Subscription(BacnetClient reciever, BacnetAddress reciever_address, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference property, bool issueConfirmedNotifications, uint lifetime, float covIncrement)
        {
            this.reciever = reciever;
            this.reciever_address = reciever_address;
            this.subscriberProcessIdentifier = subscriberProcessIdentifier;
            this.monitoredObjectIdentifier = monitoredObjectIdentifier;
            this.monitoredProperty = property;
            this.issueConfirmedNotifications = issueConfirmedNotifications;
            this.lifetime = lifetime;
            this.start = DateTime.Now;
            this.covIncrement = covIncrement;
        }
        public int GetTimeRemaining()
        {

            if (lifetime == 0) return 0;

            uint elapse = (uint)(DateTime.Now - start).TotalSeconds;

            if (lifetime > elapse)
                return (int)(lifetime - elapse);
            else

                return -1;
                
        }
    }
    public static class SubscriptionManager
    {
        private static Dictionary<BacnetObjectId, List<Subscription>> m_subscriptions = new Dictionary<BacnetObjectId, List<Subscription>>();

        public static void RemoveOldSubscriptions()
        {
            LinkedList<BacnetObjectId> to_be_deleted = new LinkedList<BacnetObjectId>();
            foreach (KeyValuePair<BacnetObjectId, List<Subscription>> entry in m_subscriptions)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    // Modif F. Chaxel <0 modifié == 0
                    if (entry.Value[i].GetTimeRemaining() < 0)
                    {
                        entry.Value.RemoveAt(i);
                        i--;
                    }
                }
                if (entry.Value.Count == 0)
                    to_be_deleted.AddLast(entry.Key);
            }
            foreach (BacnetObjectId obj_id in to_be_deleted)
                m_subscriptions.Remove(obj_id);
        }

        public static void RemoveReceiver(BacnetAddress reciever_address)
        {
            foreach (KeyValuePair<BacnetObjectId, List<Subscription>> entry in m_subscriptions)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    if (entry.Value[i].reciever_address.Equals(reciever_address))
                        entry.Value[i].lifetime=0;
                }
            }
            // will be removed on the next call to RemoveOldSubscriptions
        }
        public static List<Subscription> GetSubscriptionsForObject(BacnetObjectId objectId)
        {
            try
            {
                List<Subscription> subs = m_subscriptions[objectId];
                return subs;
            }
            catch
            { return null; }
        }

        public static Subscription HandleSubscriptionRequest(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, uint property_id, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement)
        {
            //remove old leftovers
            RemoveOldSubscriptions();

            //find existing
            List<Subscription> subs = null;
            Subscription sub = null;
            if (m_subscriptions.ContainsKey(monitoredObjectIdentifier))
            {
                subs = m_subscriptions[monitoredObjectIdentifier];
                foreach (Subscription s in subs)
                {
                    // Modif FC
                    if (s.reciever.Equals(sender) && s.reciever_address.Equals(adr) && s.monitoredObjectIdentifier.Equals(monitoredObjectIdentifier) && s.monitoredProperty.propertyIdentifier == property_id)
                    {
                        sub = s;
                        break;
                    }
                }
            }

            //cancel
            if (cancellationRequest && sub != null)
            {
                subs.Remove(sub);
                if (subs.Count == 0)
                    m_subscriptions.Remove(sub.monitoredObjectIdentifier);

                //send confirm
                // F. Chaxel : a supprimer, c'est fait par l'appellant
                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invoke_id);

                return null;
            }

            //create if needed
            if (sub == null)
            {
                sub = new Subscription(sender, adr, subscriberProcessIdentifier, monitoredObjectIdentifier, new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL), issueConfirmedNotifications, lifetime, covIncrement);

                if (subs == null)
                {
                    subs = new List<Subscription>();
                    m_subscriptions.Add(sub.monitoredObjectIdentifier, subs);
                }
                subs.Add(sub);
            }

            //update perhaps
            sub.issueConfirmedNotifications = issueConfirmedNotifications;
            sub.lifetime = lifetime;
            sub.start = DateTime.Now;

            return sub;
        }
    }
}
