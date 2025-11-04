namespace Tests;

public interface ITest
{
    IQueryable<T> AddFilter<T>(IQueryable<T> qry)
        where T : notnull;
}

public class Test : ITest
{
    public IQueryable<T> AddFilter<T>(IQueryable<T> qry)
        where T : notnull => qry;
}
