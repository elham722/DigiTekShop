#!/bin/sh
redis-cli -a "$REDIS_PASS" PING | grep -q PONG
