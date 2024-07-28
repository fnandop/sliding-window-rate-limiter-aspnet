using StackExchange.Redis;

namespace SlidingWindowRateLimiter
{
    public static class Scripts
    {
        public static LuaScript SlidingRateLimiterScript => LuaScript.Prepare(SlidingRateLimiter);
        private const string SlidingRateLimiter = @"
            local current_time = redis.call('TIME')
            local current_time_ms = current_time[1]*1000 + current_time[2]/1000
            local window_ms = @window*1000
            local trim_time = tonumber(current_time_ms) - window_ms
            redis.call('ZREMRANGEBYSCORE', @key, 0, trim_time)
            local request_count = redis.call('ZCARD',@key)

            if request_count < tonumber(@max_requests) then
                redis.call('ZADD', @key, current_time_ms, current_time[1] .. current_time[2])
                redis.call('EXPIRE', @key, @window)
                return 0
            end
            return 1
            ";

        public static LuaScript SlidingRateLimiterWaitUntilScript => LuaScript.Prepare(SlidingRateLimiterWaitUntil);
        private const string SlidingRateLimiterWaitUntil = @"
            local current_time = redis.call('TIME')
            local current_time_ms = current_time[1]*1000 + current_time[2]/1000
            local unit_wait_time_ms = 100
            local current_wait_until_ms  = redis.call('GET', 'CURRENT_WAIT_UNTIL_MS')
            if not current_wait_until_ms then
              current_wait_until_ms = 0
            end
            current_wait_until_ms = math.max(current_time_ms,current_wait_until_ms + unit_wait_time_ms )

            redis.call('SET', 'CURRENT_WAIT_UNTIL_MS',current_wait_until_ms)
            redis.call('EXPIRE', 'CURRENT_WAIT_UNTIL_MS', 1)
            return current_wait_until_ms - current_time_ms
             ";
    }
}
