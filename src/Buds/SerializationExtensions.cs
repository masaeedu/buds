using System;
using System.Collections;
using System.Collections.Generic;
using AgentAutomation.PeerCooperation.Messages;
using Newtonsoft.Json;

namespace AgentAutomation.PeerCooperation
{
    public static class SerializationExtensions
    {
        public static string SerializeAsJson(this object message)
        {
            return JsonConvert.SerializeObject(message, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        //public static string SerializeAsJson(this ExceptionResponse message)
        //{
        //    return SerializeAsJson((object)new ExceptionResponse(message.SenderNodeId, message.DestinationNodeId, message.RequestId, new SerializableException(message.Exception.Message, message.Exception)));
        //}

        public static T DeserializeFromJson<T>(this string data)
        {
            return JsonConvert.DeserializeObject<T>(data, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }
    }
}
