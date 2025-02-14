namespace Tests;

public interface Itest
{
    IQueryable<T> AddFilter<T>(IQueryable<T> qry)
        where T : notnull;
}

public class test : Itest
{
    public IQueryable<T> AddFilter<T>(IQueryable<T> qry)
        where T : notnull => qry;
}
