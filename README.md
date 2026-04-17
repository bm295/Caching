# Distributed Cache Service

A production-style distributed cache API built with ASP.NET Core. It uses **Rendezvous Hashing** to shard keys across cache peers and replicates writes for resilience.

## Features

- REST API for set/get/delete cache entries.
- Rendezvous hashing for deterministic key placement.
- Configurable replication factor.
- Optional Redis-backed local store with in-memory fallback.
- Peer-aware reads with automatic failover.
- Unit tests for hashing and replication behavior.

## Quick start

```bash
dotnet restore
dotnet run --project src/DistributedCache.Api
```

Set peers and local node ID using environment variables:

- `CacheCluster__NodeId=node-a`
- `CacheCluster__Peers__0=node-a=http://localhost:8080`
- `CacheCluster__Peers__1=node-b=http://localhost:8081`
- `CacheCluster__ReplicationFactor=2`
- `ConnectionStrings__Redis=localhost:6379` (optional)

## Web demo

The API now serves a browser-based dashboard at `/` that demonstrates the Redis calls already implemented in this repo:

- `ConnectionMultiplexer.Connect`
- `IDatabase.StringSetAsync`
- `IDatabase.StringGetAsync`
- `IDatabase.KeyDeleteAsync`

From the dashboard you can:

- Write values with an optional TTL.
- Read and delete cache entries.
- Inspect rendezvous-hash placement for a key.
- View which owner replicas currently hold the value.
- See whether the node is running against Redis or the in-memory fallback.

For the full cluster demo, start the compose stack and open any node in the browser:

```bash
docker compose up --build
```

- `http://localhost:8080`
- `http://localhost:8081`
- `http://localhost:8082`

## API

- `PUT /cache/{key}`
- `GET /cache/{key}`
- `DELETE /cache/{key}`
- `GET /cluster/placement/{key}`
- `GET /demo/api/overview`
- `GET /demo/api/inspect/{key}`
- `GET /health/live`

Payload for PUT:

```json
{
  "value": "any string payload",
  "ttlSeconds": 60
}
```
