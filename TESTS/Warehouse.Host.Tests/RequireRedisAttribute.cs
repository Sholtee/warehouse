/********************************************************************************
* RequireRedisAttribute.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

using StackExchange.Redis;

namespace Warehouse.Host.Tests
{
    internal interface IHasRedisConnection
    {
        IConnectionMultiplexer RedisConnection { get; set; }
    }

    internal sealed class RequireRedisAttribute() : RequireExternalServiceAttribute("redis:7.4.1", 6379, "test_redis")
    {
        private ConnectionMultiplexer FConnection = null!;

        protected override bool TryConnect(object fixture)
        {
            try
            {
                FConnection = ConnectionMultiplexer.Connect("localhost,allowAdmin=true");

                if (fixture is IHasRedisConnection hasRedisConnection)
                    hasRedisConnection.RedisConnection = FConnection;

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Cannot connect to Redis instance: {e.Message}");
                return false;
            }
        }

        protected override void CloseConnection()
        {
            FConnection?.Dispose();
            FConnection = null!;
        }

        protected override void TearDownTest()
        {
            foreach (IServer server in FConnection.GetServers())
            {
                server.FlushAllDatabases();
            }
        }
    }
}
