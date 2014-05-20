using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Mischel.Collections;

namespace pqTest
{
    enum AlarmEventType
    {
        Test,
        Trouble,
        Alert,
        Fire,
        Panic
    };

    class AlarmEvent
    {
        private AlarmEventType etype;
        private string msg;
        public AlarmEvent(AlarmEventType type, string message)
        {
            etype = type;
            msg = message;
        }

        public AlarmEventType EventType
        {
            get { return etype; }
        }

        public string Message
        {
            get { return msg; }
        }
    }

    class pqTest
    {
        static void Main(string[] args)
        {
            PriorityQueue<AlarmEvent, AlarmEventType> pq = 
                new PriorityQueue<AlarmEvent, AlarmEventType>();

            // Add a bunch of events to the queue
            pq.Enqueue(new AlarmEvent(AlarmEventType.Test, "Testing 1"), AlarmEventType.Test);
            pq.Enqueue(new AlarmEvent(AlarmEventType.Fire, "Fire alarm 1"), AlarmEventType.Fire);
            pq.Enqueue(new AlarmEvent(AlarmEventType.Trouble, "Battery low"), AlarmEventType.Trouble);
            pq.Enqueue(new AlarmEvent(AlarmEventType.Panic, "I've fallen and I can't get up!"), AlarmEventType.Panic);
            pq.Enqueue(new AlarmEvent(AlarmEventType.Test, "Another test."), AlarmEventType.Test);
            pq.Enqueue(new AlarmEvent(AlarmEventType.Alert, "Oops, I forgot the reset code."), AlarmEventType.Alert);

            Console.WriteLine("The queue contains {0} events", pq.Count);

            // Now remove the items in priority order
            Console.WriteLine();
            while (pq.Count > 0)
            {
                PriorityQueueItem<AlarmEvent, AlarmEventType> item = pq.Dequeue();
                Console.WriteLine("{0}: {1}", item.Priority, item.Value.Message);
            }
        }
    }
}
