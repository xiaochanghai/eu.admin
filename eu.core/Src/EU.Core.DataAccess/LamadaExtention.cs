using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EU.Core.DataAccess;

public class LamadaExtention<Dto> where Dto : class
{
    private List<Expression> m_lstExpression = null;
    private ParameterExpression m_Parameter = null;

    public LamadaExtention()
    {
        m_lstExpression = new List<Expression>();
        m_Parameter = Expression.Parameter(typeof(Dto), "x");
    }

    //构造表达式，存放到m_lstExpression集合里面
    public void GetExpression(string strPropertyName, object strValue, ExpressionType expressType)
    {
        Expression expRes = null;
        MemberExpression member = Expression.PropertyOrField(m_Parameter, strPropertyName);
        if (expressType == ExpressionType.Contains)
        {
            expRes = Expression.Call(member, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(strValue));
        }
        else if (expressType == ExpressionType.Equal)
        {
            Expression right = null;
            if (member.Type == typeof(Boolean))
                right = Expression.Constant(Convert.ToBoolean(strValue), member.Type);
            else if (member.Type == typeof(Guid) || member.Type == typeof(Guid?))
                right = Expression.Constant(Guid.Parse(strValue.ToString()), member.Type);
            else if (member.Type == typeof(int) || member.Type == typeof(int?))
                right = Expression.Constant(int.Parse(strValue.ToString()), member.Type);
            else if (member.Type == typeof(bool) || member.Type == typeof(bool?))
                right = Expression.Constant(Convert.ToBoolean(strValue), member.Type);
            else
                right = Expression.Constant(strValue, member.Type);
            expRes = Expression.Equal(member, right);
        }
        else if (expressType == ExpressionType.NotEqual)
        {
            Expression right = null;
            if (member.Type == typeof(Guid) || member.Type == typeof(Guid?))
                right = Expression.Constant(Guid.Parse(strValue.ToString()), member.Type);
            expRes = Expression.NotEqual(member, right);
        }
        else if (expressType == ExpressionType.LessThan)
        {
            expRes = Expression.LessThan(member, Expression.Constant(strValue, member.Type));
        }
        else if (expressType == ExpressionType.LessThanOrEqual)
        {
            expRes = Expression.LessThanOrEqual(member, Expression.Constant(strValue, member.Type));
        }
        else if (expressType == ExpressionType.GreaterThan)
        {
            expRes = Expression.GreaterThan(member, Expression.Constant(strValue, member.Type));
        }
        else if (expressType == ExpressionType.GreaterThanOrEqual)
        {
            expRes = Expression.GreaterThanOrEqual(member, Expression.Constant(strValue, member.Type));
        }
        //return expRes;
        m_lstExpression.Add(expRes);
    }

    //针对Or条件的表达式
    public void GetExpression(string strPropertyName, List<object> lstValue)
    {
        Expression expRes = null;
        MemberExpression member = Expression.PropertyOrField(m_Parameter, strPropertyName);
        foreach (var oValue in lstValue)
        {
            if (expRes == null)
            {
                expRes = Expression.Equal(member, Expression.Constant(oValue, member.Type));
            }
            else
            {
                expRes = Expression.Or(expRes, Expression.Equal(member, Expression.Constant(oValue, member.Type)));
            }
        }


        m_lstExpression.Add(expRes);
    }

    //多个字段or同一个值
    public void GetExpression(List<string> listStrPropertyName, object strValue, ExpressionType expressType)
    {
        Expression expRes = null;

        foreach (var itemValue in listStrPropertyName)
        {
            MemberExpression member = Expression.PropertyOrField(m_Parameter, itemValue);
            if (expressType == ExpressionType.Contains)
            {
                if (expRes == null)
                {
                    expRes = Expression.Call(member, typeof(string).GetMethod("Contains"), Expression.Constant(strValue));
                    //expRes = Expression.Equal(member, Expression.Constant(strValue, member.Type));
                }
                else
                {
                    expRes = Expression.Or(expRes, Expression.Call(member, typeof(string).GetMethod("Contains"), Expression.Constant(strValue)));
                }
            }
            else
            {
                if (expRes == null)
                {
                    expRes = Expression.Equal(member, Expression.Constant(strValue, member.Type));
                }
                else
                {
                    expRes = Expression.Or(expRes, Expression.Equal(member, Expression.Constant(strValue, member.Type)));
                }
            }
        }
        m_lstExpression.Add(expRes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="expressType"></param>
    public void GetExpression(Dictionary<string, object> list, ExpressionType expressType = ExpressionType.Contains)
    {
        Expression expRes = null;

        foreach (var item in list)
        {
            string strValue = item.Value.ToString().Trim();
            if (string.IsNullOrEmpty(strValue))
                continue;
            MemberExpression member = Expression.PropertyOrField(m_Parameter, item.Key);
            if (expressType == ExpressionType.Contains)
            {
                if (expRes == null)
                {
                    expRes = Expression.Call(member, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(strValue));
                }
                else
                {
                    expRes = Expression.Or(expRes, Expression.Call(member, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(strValue)));
                }
            }
            else
            {
                if (expRes == null)
                {
                    expRes = Expression.Equal(member, Expression.Constant(strValue, member.Type));
                }
                else
                {
                    expRes = Expression.Or(expRes, Expression.Equal(member, Expression.Constant(strValue, member.Type)));
                }
            }
        }
        m_lstExpression.Add(expRes);
    }

    //得到Lamada表达式的Expression对象
    public Expression<Func<Dto, bool>> GetLambda()
    {
        Expression whereExpr = null;
        foreach (var expr in this.m_lstExpression)
        {
            if (whereExpr == null) whereExpr = expr;
            else whereExpr = Expression.And(whereExpr, expr);
        }
        if (whereExpr == null)
            return null;
        return Expression.Lambda<Func<Dto, Boolean>>(whereExpr, m_Parameter);
    }

    /// <summary>
    /// 获取排序Lambda（如果动态排序，类型不同会导致转换失败）
    /// </summary>
    /// <typeparam name="T">数据字段类型</typeparam>
    /// <typeparam name="Tkey">排序字段类型</typeparam>
    /// <param name="defaultSort">默认的排序字段</param>
    /// <param name="sort">当前排序字段</param>
    /// <returns></returns>
    public static Expression<Func<Dto, Tkey>> SortLambda<T, Tkey>(string defaultSort, string sort)
    {
        //1.创建表达式参数（指定参数或变量的类型:p）  
        var param = Expression.Parameter(typeof(Dto), "t");
        //2.构建表达式体(类型包含指定的属性:p.Name)  
        var body = Expression.Property(param, string.IsNullOrEmpty(sort) ? defaultSort : sort);
        //3.根据参数和表达式体构造一个lambda表达式  
        return Expression.Lambda<Func<Dto, Tkey>>(Expression.Convert(body, typeof(Tkey)), param);
    }
}
//用于区分操作的枚举
public enum ExpressionType
{
    //Guid,
    //Boolean,
    Contains,//like
    Equal,//等于
    NotEqual,//不等于
    LessThan,//小于
    LessThanOrEqual,//小于等于
    GreaterThan,//大于
    GreaterThanOrEqual,//大于等于
}
