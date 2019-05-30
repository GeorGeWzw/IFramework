﻿using System.Collections.Generic;

namespace IFramework.MessageQueue.ConfluentKafka.MessageFormat
{
    public class KafkaMessage
    {
        public KafkaMessage(string payload = null)
        {
            Headers = new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            Payload = payload;
        }

        public IDictionary<string, object> Headers { get; }
        public string Payload { get; set; }
    }
}