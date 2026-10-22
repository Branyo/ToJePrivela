using System.Linq.Expressions;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Tests.Common;

public class PredicatesTests
{
    [Fact]
    public void And_HoldsOnlyWhereBothHold_OverOneParameter()
    {
        Expression<Func<int, bool>> positive = n => n > 0;
        Expression<Func<int, bool>> even = x => x % 2 == 0;

        var both = positive.And(even);

        Assert.Single(both.Parameters);
        Assert.Equal([2, 4], new[] { -2, 1, 2, 3, 4 }.AsQueryable().Where(both));
    }
}
