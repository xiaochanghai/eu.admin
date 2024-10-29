using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EU.Core.Domain;

public interface IBaseCRUDVM<TModel> where TModel : class
{
    IEnumerable<TModel> Get();
    TModel Get(Expression<Func<TModel, bool>> express = null);

    TModel GetById(object id);
    Task<TModel> GetByIdAsync(object id);

    void DoAdd(TModel model);

    Task DoAddAsync(TModel model);

    void DoDelete(object id);

    Task DoDeleteAsync(Guid id, Guid? updateById = null);

    void DoRealDelete(object id);

    Task DoRealDeleteAsync(object id);

    void DoUpdate(TModel model);

    Task DoUpdateAsync(TModel model);
    Task<TModel> GetAsync(Expression<Func<TModel, bool>> express);
}
