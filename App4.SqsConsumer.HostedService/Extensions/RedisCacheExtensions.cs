using System.Reflection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace App4.SqsConsumer.HostedService.Extensions
{
    public static class RedisCacheExtensions
    {

        public static ConnectionMultiplexer GetConnection(this RedisCache cache)
        {
            //ensure connection is established
            typeof(RedisCache).InvokeMember("Connect", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, cache, new object[] { });

            //get connection multiplexer
            var fi = typeof(RedisCache).GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic);
            var connection = (ConnectionMultiplexer)fi.GetValue(cache);
            return connection;
        }
    }
}
