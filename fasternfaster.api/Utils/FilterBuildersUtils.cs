using System.Linq.Expressions;

namespace FasterNFaster.Api.Utils;

public static class FilterBuildersUtils
{
    public static IQueryable<T> GetTopPlayersFilter<T>(IQueryable<T> source, string propertyName, bool isDescending, int count = 100)
    {
        var parameter = Expression.Parameter(typeof(T), "p");

        var property = Expression.Property(parameter, propertyName);

        var lambda = Expression.Lambda(property, parameter);

        var orderByCall = Expression.Call(
            typeof(Queryable),
            isDescending ? "OrderByDescending" : "OrderBy",
            new Type[] { typeof(T), property.Type },
            source.Expression,
            Expression.Quote(lambda)
        );

        var takeCall = Expression.Call(
            typeof(Queryable),
            "Take",
            new Type[] { typeof(T) },
            orderByCall,
            Expression.Constant(count)
        );

        return source.Provider.CreateQuery<T>(takeCall);
    }
}