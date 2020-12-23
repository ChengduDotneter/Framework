namespace TestWebAPI.Lock
{
    public static class RedisLockLuaScript
    {
        public const string ACQUIRE_NREAD_NWRITE_LOCK = @"-- 多读多写读写锁申请
local transcation_id = KEYS[1] -- 事务ID
local exp_time = KEYS[2] -- 超时时间 单位 毫秒
local resource_read = KEYS[3] -- 表的读锁Key
local resource_write = KEYS[4] -- 表的写锁Key
local lock_mode = KEYS[5] -- 读写锁类型

local is_locked = 0 -- 标识整个锁操作的成功与失败

if(lock_mode == '0') then
	if( redis.call('EXISTS',resource_write) == 0 or ( redis.call('HLEN',resource_write) == 1 and redis.call('HEXISTS',resource_write,transcation_id) == 1 ) ) then -- 如果该表的读锁键不存在 或者 该读锁键存在且仅有一条记录，并且该记录能与当前事务ID匹配时 可进行写锁申请
		redis.call('HSETNX',resource_read,transcation_id,1) -- 向表的写锁hash中添加一条记录
		redis.call('PEXPIRE',resource_read,exp_time) -- 为该hash键设置生命时间
		is_locked = 1 -- 将上锁标识置为1
	end
elseif (lock_mode == '1') then
	if( redis.call('EXISTS',resource_read) == 0 or ( redis.call('HLEN',resource_read) == 1 and redis.call('HEXISTS',resource_read,transcation_id) == 1 ) ) then -- 如果该表的读锁键不存在 或者 该读锁键存在且仅有一条记录，并且该记录能与当前事务ID匹配时 可进行写锁申请
		redis.call('HSETNX',resource_write,transcation_id,1) -- 向表的写锁hash中添加一条记录
		redis.call('PEXPIRE',resource_write,exp_time) -- 为该hash键设置生命时间
		is_locked = 1 -- 将上锁标识置为1
	end	
else
	is_locked = 0
end

return is_locked";

        public const string ACQUIRE_NREAD_ONEWRITE_LOCK = @"-- 一写多读读写锁申请
local transcation_id = KEYS[1] -- 事务ID
local exp_time = KEYS[2] -- 超时时间 单位 毫秒
local lock_mode = KEYS[3] -- 锁的模式 0 读锁 1 行锁
local lock_group_read = KEYS[4] -- 表的读锁Key
local lock_group_write = KEYS[5] -- 表的写锁Key
local lock_resource_length = tonumber(KEYS[6]) -- 需要上锁的资源数

local is_locked = 0 -- 标识整个锁操作的成功与失败

if( redis.call('EXISTS',lock_group_write) == 0 or ( redis.call('HLEN',lock_group_write) == 1 and redis.call('HEXISTS',lock_group_write,transcation_id) == 1 ) ) then  -- 如果该表的写键不存在 或者 该写锁键存在且仅有一条记录，并且该记录能与当前事务ID匹配时 可进行行锁申请
	redis.call('HSETNX',lock_group_read,transcation_id,1) -- 向表的读锁hash中添加一条记录
	redis.call('PEXPIRE',lock_group_read,exp_time) -- 为该hash键设置生命时间
else 
	return is_locked
end

if( lock_resource_length > 0 ) then
	if( lock_mode == '0' ) then -- 如果申请的锁为读锁
		for i = 7,7 + lock_resource_length - 1 do -- 对需要锁的资源进行循环
			local lock_resource_read = KEYS[i]  -- 构建行的读锁键
			local lock_resource_write = KEYS[i + lock_resource_length] -- 构建行的写锁键
			
			if( redis.call('EXISTS',lock_resource_write) == 0 or  ( redis.call('HLEN',lock_resource_write) == 1 and redis.call('HEXISTS',lock_resource_write,transcation_id) == 1 ) ) then -- 判断读锁互斥的写锁键不存在 或者 该写锁键存在且仅有一条记录，并且该记录能与当前事务ID匹配时
				redis.call('HSETNX',lock_resource_read,transcation_id,1) -- 向行的读锁hash中添加一条记录
				redis.call('PEXPIRE',lock_resource_read,exp_time) -- 为该hash键设置生命时间
				is_locked = 1 -- 将上锁标识置为1
			else  -- 否则就返回申请行锁失败
				is_locked = 0 -- 将上锁标识置为0
				return is_locked
			end
		end
	
	elseif ( lock_mode == '1' ) then -- 如果申请的锁为写锁
		for i = 7,7 + lock_resource_length - 1 do -- 对需要锁的资源进行循环
			local lock_resource_read = KEYS[i]  -- 构建行的读锁键
			local lock_resource_write = KEYS[i + lock_resource_length] -- 构建行的写锁键
			
			if( redis.call('EXISTS',lock_resource_read) == 0 or ( redis.call('HLEN',lock_resource_read) == 1 and redis.call('HEXISTS',lock_resource_read,transcation_id) == 1 ) ) then -- 判断写锁互斥的读锁键不存在 或者 该读锁键存在且仅有一条记录，并且该记录能与当前事务ID匹配时
				if( redis.call('EXISTS',lock_resource_write) == 0 or ( redis.call('HLEN',lock_resource_write) == 1 and redis.call('HEXISTS',lock_resource_write,transcation_id) == 1 ) ) then -- 判断写锁互斥的写锁键不存在 或者 该写锁键存在且仅有一条记录，并且该记录能与当前事务ID匹配时
					redis.call('HSETNX',lock_resource_write,transcation_id, 1) -- 向行的写锁hash中添加一条记录,并刷新其存活时间
					redis.call('PEXPIRE',lock_resource_write,exp_time) -- 为该hash键设置生命时间
					is_locked = 1 -- 将上锁标识置为1
				else  -- 如果存在该资源的写锁 且不是当前线程 则向表的写锁键添加一条记录
					is_locked = 0 -- 将上锁标识置为0
					return is_locked
				end
			else -- 如果存在读锁 并且读锁的并不仅为当前线程使用，则返回行锁申请失败
				is_locked = 0 -- 将上锁标识置为0
				return is_locked
			end
		end
	end
	else 
		is_locked = 0
end

return is_locked";




        public const string Release_DB_LOCK = @"-- 锁释放
local transcation_id = KEYS[1] -- 事务ID
local lock_resources_length = tonumber(KEYS[2]) --锁的资源长度

if( lock_resources_length > 0 ) then
	for i = 3,3 + lock_resources_length - 1 do -- 对释放的行锁资源进行循环
		if( redis.call('HEXISTS',KEYS[i],transcation_id) == 1 ) then --如果存在该资源锁并且该锁的事务id等于该事务id，则移除资源锁，释放该资源
			redis.call('HDEL',KEYS[i],transcation_id)
		end
	end
end";

        public const string SAVE_DB_LOCK = @"-- 锁维持
local monitor_time = tonumber(KEYS[1]) -- 监测时间 单位 毫秒
local exp_time = KEYS[2] -- 监测时间 单位 毫秒
local lock_resources_length = tonumber(KEYS[3]) --锁的资源长度

if( lock_resources_length > 0) then
	for i = 4,4 + lock_resources_length - 1  do -- 对需要释放的表的读锁资源进行循环
		if(redis.call('EXISTS', KEYS[i]) == 0) then
			return 0
		end

		if(redis.call('PTTL', KEYS[i]) <= monitor_time) then
			redis.call('PEXPIRE',KEYS[i],exp_time)
		end
	end
end

return 1
";

        //		public const string SAVE_DB_LOCK = @"-- 锁维持
        //local monitor_time = tonumber(KEYS[1]) -- 监测时间 单位 毫秒
        //local exp_time = KEYS[2] -- 监测时间 单位 毫秒
        //local lock_resources_length = tonumber(KEYS[3]) --锁的资源长度

        //if( lock_resources_length > 0) then
        //	for i = 4,4 + lock_resources_length - 1  do -- 对需要释放的表的读锁资源进行循环
        //		if(redis.call('PTTL', KEYS[i]) <= monitor_time) then
        //			redis.call('PEXPIRE',KEYS[i],exp_time)
        //		end
        //	end
        //end

        //return 1
        //";
    }
}
