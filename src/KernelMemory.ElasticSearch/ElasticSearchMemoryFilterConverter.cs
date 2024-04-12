using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.KernelMemory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KernelMemory.ElasticSearch;

internal static class ElasticSearchMemoryFilterConverter
{
    internal static QueryDescriptor<object> CreateQueryDescriptorFromMemoryFilter(
        IEnumerable<MemoryFilter>? filters)
    {
        //need to get all filters that have conditions
        var realFilters = filters?
            .Where(filters => filters.GetFilters().Any(f => !string.IsNullOrEmpty(f.Value)))?
            .ToList() ?? [];

        if (realFilters.Count == 0)
        {
            return new QueryDescriptor<object>().MatchAll(new MatchAllQuery());
        }

        //ok I really have some conditions, we need to build the querydescriptor.
        if (realFilters.Count == 1)
        {
            return ConvertFilterToQueryDescriptor(realFilters[0]);
        }

        //ok we have really more than one filter, convert all filter to Query object than finally return 
        //a composition with OR
        var convertedFilters = realFilters
            .Select(ConvertFilterToQuery)
            .ToArray();

        return new QueryDescriptor<object>().Bool(new BoolQuery()
        {
            Should = convertedFilters
        });
    }

    private static QueryDescriptor<object> ConvertFilterToQueryDescriptor(MemoryFilter filter)
    {
        var innerFilters = filter.GetFilters().Where(f => !string.IsNullOrEmpty(f.Value)).ToArray();

        //lets double check if this filter really has conditions.
        if (innerFilters.Length == 0)
        {
            return new QueryDescriptor<object>().MatchAll(new MatchAllQuery());
        }

        if (innerFilters.Length == 1)
        {
            var f = innerFilters[0];
            return new QueryDescriptor<object>().Match(TagMatchQuery(f));
        }

        //ok we have more than one condition, we need to build a bool query.
        List<Query> convertedFilters = new();
        foreach (var f in innerFilters)
        {
            if (!String.IsNullOrEmpty(f.Value))
            {
                convertedFilters.Add(Query.Match(TagMatchQuery(f)));
            }
        }

        return new QueryDescriptor<object>().Bool(bq => bq.Must(convertedFilters.ToArray()));
    }

    private static Query ConvertFilterToQuery(MemoryFilter filter)
    {
        var innerFilters = filter.GetFilters().Where(f => !string.IsNullOrEmpty(f.Value)).ToArray();

        //lets double check if this filter really has conditions.
        if (innerFilters.Length == 0)
        {
            return Query.MatchAll(new MatchAllQuery());
        }

        if (innerFilters.Length == 1)
        {
            var f = innerFilters[0];
            return Query.Match(TagMatchQuery(f));
        }

        //ok we have more than one condition, we need to build a bool query.
        List<Query> convertedFilters = new();
        foreach (var f in innerFilters)
        {
            if (!String.IsNullOrEmpty(f.Value))
            {
                convertedFilters.Add(Query.Match(TagMatchQuery(f)));
            }
        }

        return Query.Bool(new BoolQuery()
        {
            Must = convertedFilters
        });
    }

    private static TermQuery TagTermQuery(KeyValuePair<string, string> f)
    {
        return new TermQuery($"tag_{f.Key}")
        {
            Value = f.Value
        };
    }

    private static MatchQuery TagMatchQuery(KeyValuePair<string, string?> f)
    {
        return new MatchQuery($"tag_{f.Key}")
        {
            Query = f.Value!
        };
    }
}
