using EU.Core.DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EU.Core.Domain;

public class BaseCRUDVM<TModel> : IBaseCRUDVM<TModel> where TModel : class
{
    private readonly DataContext _context;
    internal DbSet<TModel> dbSet;

    public BaseCRUDVM(DataContext context)
    {
        _context = context;
        this.dbSet = _context.Set<TModel>();
    }

    IEnumerable<TModel> IBaseCRUDVM<TModel>.Get()
    {
        return dbSet.ToList();
    }

    TModel IBaseCRUDVM<TModel>.Get(Expression<Func<TModel, bool>> express)
    {
        return dbSet.Where(express).FirstOrDefault();
    }

    async Task<TModel> IBaseCRUDVM<TModel>.GetAsync(Expression<Func<TModel, bool>> express)
    {
        return await dbSet.Where(express).FirstOrDefaultAsync();
    }

    public TModel GetById(object id)
    {
        return dbSet.Find(id);
    }

    public async Task<TModel> GetByIdAsync(object id)
    {
        return await dbSet.FindAsync(id);
    }

    public void DoAdd(TModel model)
    {
        _context.Entry(model).CurrentValues["IsDeleted"] = false;
        _context.Entry(model).CurrentValues["AuditStatus"] = "Add";
        _context.Add(model);
        _context.SaveChanges();
    }

    public async Task DoAddAsync(TModel model)
    {
        _context.Entry(model).CurrentValues["IsDeleted"] = false;
        _context.Entry(model).CurrentValues["AuditStatus"] = "Add";
        await _context.AddAsync(model);
        await _context.SaveChangesAsync();
    }

    public void DoDelete(object id)
    {
        var query = dbSet.Find(id);
        _context.Entry(query).CurrentValues["IsDeleted"] = true;
        _context.Update(query);
        _context.SaveChanges();
    }

    public async Task DoDeleteAsync(Guid id, Guid? updateById = null)
    {
        var query = await dbSet.FindAsync(id);
        _context.Entry(query).CurrentValues["IsDeleted"] = true;
        _context.Entry(query).CurrentValues["UpdateBy"] = updateById ?? null;
        _context.Entry(query).CurrentValues["UpdateTime"] = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public void DoRealDelete(object id)
    {
        var query = dbSet.Find(id);
        _context.Remove(query);
        _context.SaveChanges();
    }

    public async Task DoRealDeleteAsync(object id)
    {
        var query = await dbSet.FindAsync(id);
        _context.Remove(query);
        await _context.SaveChangesAsync();
    }

    public void DoUpdate(TModel model)
    {
        _context.Update(model);
        _context.SaveChanges();
    }

    public async Task DoUpdateAsync(TModel model)
    {
        _context.Update(model);
        await _context.SaveChangesAsync();
    }


}
