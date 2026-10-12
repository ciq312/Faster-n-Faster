using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FasterNFaster.IntegrationTests;

public class DbCommandCounter : DbCommandInterceptor
{
    private int count;

    public int Count => Volatile.Read(ref count);

    public override DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
    {
        Interlocked.Increment(ref count);
        return result;
    }
}
