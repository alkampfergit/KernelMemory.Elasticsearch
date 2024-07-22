using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.KernelMemory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KernelMemory.ElasticSearch;

internal static class ElasticSearchMemoryFilterConverter
{
    internal static QueryDescriptor<object> CreateQueryDescriptorFromMemoryFilter(
        IEnumerable<MemoryFilter>? filters)
    {
        List<MemoryFilter> allFilters = GetAllRealFilters(filters);

        //check if we have no filters.
        if (allFilters.Count == 0)
        {
            return new QueryDescriptor<object>().MatchAll(new MatchAllQuery());
        }

        //ok I really have some conditions, we need to build the querydescriptor.
        if (allFilters.Count == 1)
        {
            return ConvertFilterToQueryDescriptor(allFilters[0]);
        }

        //ok we have really more than one filter, convert all filter to Query object than finally return 
        //a composition with OR
        var convertedFilters = allFilters
            .Select(ConvertFilterToQuery)
            .ToArray();

        return new QueryDescriptor<object>().Bool(new BoolQuery()
        {
            Should = convertedFilters
        });
    }

    internal static Query CreateQueryFromMemoryFilter(
        IEnumerable<MemoryFilter>? filters)
    {
        List<MemoryFilter> allFilters = GetAllRealFilters(filters);

        if (allFilters.Count == 0)
        {
            return Query.MatchAll(new MatchAllQuery());
        }

        //ok I really have some conditions, we need to build the querydescriptor.
        if (allFilters.Count == 1)
        {
            return ConvertFilterToQuery(allFilters[0]);
        }

        //ok we have really more than one filter, convert all filter to Query object than finally return 
        //a composition with OR
        var convertedFilters = allFilters
            .Select(ConvertFilterToQuery)
            .ToArray();

        return Query.Bool(new BoolQuery()
        {
            Should = convertedFilters
        });
    }

    private static List<MemoryFilter> GetAllRealFilters(IEnumerable<MemoryFilter>? filters)
    {
        //need to get all filters that have conditions
        var equalFilters = filters?
            .Where(filters => filters.GetFilters().Any(f => !string.IsNullOrEmpty(f.Value)))?
            .ToList() ?? [];

        var notFilters = filters?
            .OfType<ExtendedMemoryFilter>()?
            .Where(filters => filters.GetNotFilters().Any(f => !string.IsNullOrEmpty(f.Value)))?
            .ToList() ?? [];

        var allFilters = equalFilters.Union(notFilters).ToList();
        return allFilters;
    }

    private record BaseFilter(string Key, string Value);

    private record EqualFilter(string Key, string Value) : BaseFilter(Key, Value);

    private record NotEqualFilter(string Key, string Value) : BaseFilter(Key, Value);

    private static QueryDescriptor<object> ConvertFilterToQueryDescriptor(MemoryFilter filter)
    {
        BaseFilter[] innerFilters = ConvertToBaseFilterArray(filter);

        //lets double check if this filter really has conditions.
        if (innerFilters.Length == 0)
        {
            return new QueryDescriptor<object>().MatchAll(new MatchAllQuery());
        }

        if (innerFilters.Length == 1)
        {
            //we have a single filter, we can do a simple match query or boolean if we have a not equal filter.
            var baseFilter = innerFilters[0];
            if (baseFilter is EqualFilter f)
            {
                var mq = new MatchQuery($"tag_{f.Key}")
                {
                    Query = f.Value!
                };
                return new QueryDescriptor<object>().Match(mq);
            }
            else if (baseFilter is NotEqualFilter nf)
            {
                var boolQuery = new BoolQuery
                {
                    MustNot = new Query[]
                    {
                     new MatchQuery($"tag_{nf.Key}")
                     {
                         Query = nf.Value!
                     }
                     }
                };
                return new QueryDescriptor<object>().Bool(boolQuery);
            }
        }

        //ok we have more than one condition, we need to build a bool query.
        List<Query> convertedFilters = new();
        foreach (var f in innerFilters)
        {
            if (!String.IsNullOrEmpty(f.Value))
            {
                convertedFilters.Add(ConvertToElasticQuery(f));
            }
        }

        return new QueryDescriptor<object>().Bool(bq => bq.Must(convertedFilters.ToArray()));
    }

    private static Query ConvertFilterToQuery(MemoryFilter filter)
    {
        var innerFilters = ConvertToBaseFilterArray(filter);

        //lets double check if this filter really has conditions.
        if (innerFilters.Length == 0)
        {
            return Query.MatchAll(new MatchAllQuery());
        }

        if (innerFilters.Length == 1)
        {
            var f = innerFilters[0];
            return ConvertToElasticQuery(f);
        }

        //ok we have more than one condition, we need to build a bool query.
        List<Query> convertedFilters = new();
        foreach (var f in innerFilters)
        {
            if (!String.IsNullOrEmpty(f.Value))
            {
                convertedFilters.Add(ConvertToElasticQuery(f));
            }
        }

        return Query.Bool(new BoolQuery()
        {
            Must = convertedFilters
        });
    }

    private static BaseFilter[] ConvertToBaseFilterArray(MemoryFilter filter)
    {
        var innerFiltersList = filter
            .GetFilters()
            .Where(f => !string.IsNullOrEmpty(f.Value))
            .Select(f => (BaseFilter)new EqualFilter(f.Key, f.Value!));

        var enhancedFilter = filter as ExtendedMemoryFilter;
        if (enhancedFilter != null)
        {
            innerFiltersList = innerFiltersList.Union(enhancedFilter
                .GetNotFilters()
                .Where(f => !string.IsNullOrEmpty(f.Value))
                .Select(f => (BaseFilter)new NotEqualFilter(f.Key, f.Value!)));
        }

        var innerFilters = innerFiltersList.ToArray();
        return innerFilters;
    }

    //private static MatchQuery TagMatchQuery(BaseFilter baseFilter)
    //{
    //    return new MatchQuery($"tag_{baseFilter.Key}")
    //    {
    //        Query = baseFilter.Value!
    //    };
    //}

    private static Query ConvertToElasticQuery(BaseFilter baseFilter)
    {
        if (baseFilter is EqualFilter f)
        {
            return new MatchQuery($"tag_{f.Key}")
            {
                Query = f.Value!
            };
        }
        else if (baseFilter is NotEqualFilter nf)
        {
            return new BoolQuery
            {
                MustNot = new Query[]
                {
                     new MatchQuery($"tag_{nf.Key}")
                     {
                         Query = nf.Value!
                     }
                 }
            };
        }

        throw new NotSupportedException($"Filter of type {baseFilter.GetType().Name} not supported");
    }
}
