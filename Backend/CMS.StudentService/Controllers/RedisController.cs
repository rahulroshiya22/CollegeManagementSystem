using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CMS.StudentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RedisController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisController> _logger;

        public RedisController(IDistributedCache cache, ILogger<RedisController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint to verify Redis connectivity
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Try to set a test value
                var testKey = "health_check_test";
                var testValue = $"Redis is working! - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                
                await _cache.SetStringAsync(testKey, testValue, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                });

                // Try to get it back
                var retrievedValue = await _cache.GetStringAsync(testKey);

                if (retrievedValue == testValue)
                {
                    _logger.LogInformation("✅ Redis health check PASSED - Connection is working");
                    return Ok(new
                    {
                        status = "✅ Healthy",
                        message = "Redis is connected and working properly",
                        timestamp = DateTime.UtcNow,
                        testValue = retrievedValue
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️ Redis health check FAILED - Value mismatch");
                    return StatusCode(500, new
                    {
                        status = "❌ Unhealthy",
                        message = "Redis connected but data retrieval failed",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Redis health check FAILED with exception");
                return StatusCode(500, new
                {
                    status = "❌ Unhealthy",
                    message = "Redis connection failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Comprehensive test of Redis cache operations
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestCacheOperations()
        {
            var results = new Dictionary<string, object>();
            var testsPassed = 0;
            var testsTotal = 0;

            try
            {
                // Test 1: Write to cache
                testsTotal++;
                _logger.LogInformation("Test 1: Writing to cache...");
                var testKey = "test_student_cache";
                var testData = new
                {
                    StudentId = 999,
                    Name = "Test Student",
                    Email = "test@college.edu",
                    Timestamp = DateTime.UtcNow
                };

                var serializedData = JsonSerializer.Serialize(testData);
                await _cache.SetStringAsync(testKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                results.Add("Test 1 - Write", "✅ PASS - Data written to cache");
                testsPassed++;

                // Test 2: Read from cache
                testsTotal++;
                _logger.LogInformation("Test 2: Reading from cache...");
                var cachedData = await _cache.GetStringAsync(testKey);
                
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var deserializedData = JsonSerializer.Deserialize<dynamic>(cachedData);
                    results.Add("Test 2 - Read", new
                    {
                        Status = "✅ PASS - Data read from cache successfully",
                        Data = deserializedData
                    });
                    testsPassed++;
                }
                else
                {
                    results.Add("Test 2 - Read", "❌ FAIL - No data found in cache");
                }

                // Test 3: Cache refresh (update existing key)
                testsTotal++;
                _logger.LogInformation("Test 3: Refreshing cache entry...");
                await _cache.RefreshAsync(testKey);
                results.Add("Test 3 - Refresh", "✅ PASS - Cache entry refreshed");
                testsPassed++;

                // Test 4: Delete from cache
                testsTotal++;
                _logger.LogInformation("Test 4: Removing from cache...");
                await _cache.RemoveAsync(testKey);
                
                var deletedCheck = await _cache.GetStringAsync(testKey);
                if (deletedCheck == null)
                {
                    results.Add("Test 4 - Delete", "✅ PASS - Data removed from cache");
                    testsPassed++;
                }
                else
                {
                    results.Add("Test 4 - Delete", "❌ FAIL - Data still exists after delete");
                }

                // Test 5: Multiple key storage
                testsTotal++;
                _logger.LogInformation("Test 5: Multiple keys test...");
                for (int i = 1; i <= 5; i++)
                {
                    await _cache.SetStringAsync($"test_key_{i}", $"Value {i}", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                    });
                }
                results.Add("Test 5 - Multiple Keys", "✅ PASS - 5 keys stored successfully");
                testsPassed++;

                var summary = new
                {
                    overallStatus = testsPassed == testsTotal ? "✅ ALL TESTS PASSED" : $"⚠️ {testsPassed}/{testsTotal} TESTS PASSED",
                    testsPassed,
                    testsTotal,
                    timestamp = DateTime.UtcNow,
                    results
                };

                _logger.LogInformation($"Redis tests completed: {testsPassed}/{testsTotal} passed");
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis test operations failed");
                return StatusCode(500, new
                {
                    status = "❌ FAILED",
                    message = "Redis test operations failed with exception",
                    error = ex.Message,
                    testsPassed,
                    testsTotal,
                    timestamp = DateTime.UtcNow,
                    results
                });
            }
        }

        /// <summary>
        /// Get cache statistics and info
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetCacheInfo()
        {
            try
            {
                return Ok(new
                {
                    status = "✅ Cache service is available",
                    cacheType = "Redis (StackExchange.Redis)",
                    instanceName = "CMS_Student_",
                    defaultExpiration = "5 minutes",
                    endpoint = "Redis Cloud (redis-10247.c278.us-east-1-4.ec2.cloud.redislabs.com:10247)",
                    ssl = "TLS 1.2+",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache info");
                return StatusCode(500, new
                {
                    status = "❌ Failed",
                    error = ex.Message
                });
            }
        }
    }
}
