using System.Xml;
using System.Xml.Serialization;

namespace Priority_Queue
{
    public class PriorityQueueNode
	{
		[XmlIgnore]
        /// <summary>
        /// The Priority to insert this node at.  Must be set BEFORE adding a node to the queue
        /// </summary>
        public double Priority { get;
            set; 
        }
		
		[XmlIgnore]
        /// <summary>
        /// <b>Used by the priority queue - do not edit this value.</b>
        /// Represents the order the node was inserted in
        /// </summary>
        public long InsertionIndex { get; set; }
		
		[XmlIgnore]
        /// <summary>
        /// <b>Used by the priority queue - do not edit this value.</b>
        /// Represents the current position in the queue
        /// </summary>
        public int QueueIndex { get; set; }
    }
}