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

## API

- `PUT /cache/{key}`
- `GET /cache/{key}`
- `DELETE /cache/{key}`
- `GET /cluster/placement/{key}`
- `GET /health/live`

Payload for PUT:

```json
{
  "value": "any string payload",
  "ttlSeconds": 60
}
```
