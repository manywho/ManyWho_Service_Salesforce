using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Microsoft.ApplicationServer.Caching;
//using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using ManyWho.Flow.SDK.Utils;

namespace ManyWho.Service.Salesforce.Singletons
{
    public class SalesforceCacheManagerSingleton
    {
        //private static SalesforceCacheManagerSingleton salesforceCacheManagerSingleton;

        //private DataCache defaultCache = null;

        //private SalesforceCacheManagerSingleton()
        //{
        //    DataCacheFactoryConfiguration config = null;
        //    DataCacheFactory cacheFactory = null;

        //    config = new DataCacheFactoryConfiguration("default");
        //    cacheFactory = new DataCacheFactory(config);
        //    defaultCache = cacheFactory.GetCache("default");
        //}

        //public static SalesforceCacheManagerSingleton GetInstance()
        //{
        //    if (salesforceCacheManagerSingleton == null)
        //    {
        //        salesforceCacheManagerSingleton = new SalesforceCacheManagerSingleton();
        //    }

        //    return salesforceCacheManagerSingleton;
        //}

        //private RetryPolicy GetCacheRetryPolicy()
        //{
        //    RetryStrategy retryStrategy = null;
        //    RetryPolicy retryPolicy = null;

        //    // Define your retry strategy: retry 3 times, 1 second apart.
        //    retryStrategy = new FixedInterval(3, TimeSpan.FromSeconds(1));

        //    // Define your retry policy using the retry strategy and the Windows Azure Caching
        //    retryPolicy = new RetryPolicy<CacheTransientErrorDetectionStrategy>(retryStrategy);

        //    return retryPolicy;
        //}

        //public Object Get(String key)
        //{
        //    Object serviceElementObject = null;

        //    if (key == null ||
        //        key.Trim().Length == 0)
        //    {
        //        throw ErrorUtils.GetWebException(System.Net.HttpStatusCode.InternalServerError, "Key value cannot be null or blank. ");
        //    }

        //    this.GetCacheRetryPolicy().ExecuteAction(() =>
        //    {
        //        serviceElementObject = defaultCache.Get(key);
        //    });

        //    return serviceElementObject;
        //}

        //public void Set(String key, Object value)
        //{
        //    if (key == null ||
        //        key.Trim().Length == 0)
        //    {
        //        throw ErrorUtils.GetWebException(System.Net.HttpStatusCode.InternalServerError, "Key value cannot be null or blank. ");
        //    }

        //    if (value == null)
        //    {
        //        throw ErrorUtils.GetWebException(System.Net.HttpStatusCode.InternalServerError, "Value value cannot be null or blank. ");
        //    }

        //    this.GetCacheRetryPolicy().ExecuteAction(() =>
        //    {
        //        defaultCache.Put(key, value);
        //    });
        //}
    }
}
