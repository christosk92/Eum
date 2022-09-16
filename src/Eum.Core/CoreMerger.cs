using System.ComponentModel.Design;
using Eum.Core.Contracts;
using Eum.Core.Contracts.Models;
using Eum.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Eum.Core;

public static class CoreMerger 
{
    public static IServiceCollection MergeCores(this IServiceCollection services)
    {
        services.AddTransient<IMergedCore>(provider =>
        {
            var getAllCores = provider.GetServices<IMusicCore>();
            return MergeCores(provider.GetService<IUnifiedIdsConfiguration>()
                ?? new GenericUnifiedIdsConfiguration(),
                getAllCores.ToArray());
        });
        return services;
    }
    
    public static IMergedCore MergeCores(IUnifiedIdsConfiguration unifiedIdsConfiguration,
        params IMusicCore[] cores)
    {
        var newMergedCore = new MergedCore(unifiedIdsConfiguration)
        {
            Cores = cores
                .ToDictionary(a => a.Type, a => a)
        };

        return newMergedCore;
    }
    public static IMergedCore MergeCores(
        params IMusicCore[] cores)
    {
        var newMergedCore = new MergedCore(new GenericUnifiedIdsConfiguration())
        {
            Cores = cores
                .ToDictionary(a => a.Type, a => a)
        };

        return newMergedCore;
    }
}

internal record MergedCore : IMergedCore
{
    private readonly IUnifiedIdsConfiguration _unifiedIdsConfiguration;

    public MergedCore(IUnifiedIdsConfiguration unifiedIdsConfiguration)
    {
        _unifiedIdsConfiguration = unifiedIdsConfiguration;
    }

    public IReadOnlyDictionary<CoreType, IMusicCore> Cores { get; init; }

    public async Task<IReadOnlyDictionary<CoreType, ICoreSearchResponse>> 
        SearchAsync(string query,
        CancellationToken ct = default)
    {
        var searchAlCoresAsync = Cores
            .Select(async a =>
            {
                try
                {
                    var searchedData = await a.Value
                        .SearchAsync(query, ct);

                    return (a.Key, searchedData);
                }
                catch (Exception x)
                {
                    return (a.Key, new ExceptionSearch(x) as ICoreSearchResponse);
                }
            });

        var searchedData = await Task.WhenAll(searchAlCoresAsync);

        return searchedData
            .ToDictionary(a => a.Key, a => a.Item2);
    }

    public IReadOnlyList<IReadOnlyDictionary<CoreType, string>> MergeIds(CoreId one, CoreId two)
    {
        _unifiedIdsConfiguration.MergeId(one, two);

        var one_l=  _unifiedIdsConfiguration
            .GetIds(one);
        var two_l = _unifiedIdsConfiguration.GetIds(two);
        return new[]
        {
            one_l,
            two_l
        };
    }
    public void MergeIds_AndForget(CoreId one, CoreId two)
    {
        _unifiedIdsConfiguration.MergeId(one, two);
    }
}

internal class ExceptionSearch : ICoreSearchResponse
{
    public ExceptionSearch(Exception exception)
    {
        Exception = exception;
    }

    public Exception Exception { get; }
    public bool IsError => true;
}