#!/bin/sh
# Redis Healthcheck Script

redis-cli -a "$REDIS_PASS" PING | grep -q PONG



