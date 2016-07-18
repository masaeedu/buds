using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Buds.Extensions
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetHandledTypes(this RemotingAgent actor)
        {
            return actor.GetType()
                .GetTypeInfo()
                .ImplementedInterfaces.Where(t => t.GetGenericTypeDefinition() == typeof(IListener<>))
                .Select(t => t.GenericTypeArguments.Single());
        }
    }
}
