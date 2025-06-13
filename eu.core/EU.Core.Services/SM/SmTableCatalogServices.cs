/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmTableCatalog.cs
*
*功 能： N / A
* 类 名： SmTableCatalog
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/22 9:47:51  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using EU.Core.Common.DB;

namespace EU.Core.Services;

/// <summary>
/// 映射表 (服务)
/// </summary>
public class SmTableCatalogServices : BaseServices<SmTableCatalog, SmTableCatalogDto, InsertSmTableCatalogInput, EditSmTableCatalogInput>, ISmTableCatalogServices
{
    private readonly IBaseRepository<SmTableCatalog> _dal;
    public SmTableCatalogServices(IBaseRepository<SmTableCatalog> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 初始化指定表
    /// <summary>
    /// 初始化指定表
    /// </summary>
    /// <param name="tableCode"></param>
    /// <returns></returns>
    public async Task<ServiceResult> InitAssignmentTable(Guid id)
    {

        var enity = await base.QueryDto(id);
        TableManager.InitTableAndField(enity.TableCode, id);

        return Success(ResponseText.EXECUTE_SUCCESS);

    }
    #endregion

    #region 初始化所有表
    /// <summary>
    /// 初始化指定表
    /// </summary>
    /// <param name="tableCode"></param>
    /// <returns></returns>
    public async Task<ServiceResult> InitAllTable()
    {
        //Task.Run(async () =>
        //{
        #region 初始化所有表
        DataTable dvUserTables = null;
        if (DBHelper.MySql)
        {
            DbSelect dsUserTables = new DbSelect("INFORMATION_SCHEMA.TABLES A", "A", null);
            dsUserTables.IsInitDefaultValue = false;
            string dbName = DBServerProvider.GetConnectionString().Split("Database=")[1].Split(";")[0]?.Trim();
            dsUserTables.Where($"A.TABLE_SCHEMA='{dbName}' AND A.TABLE_TYPE='BASE TABLE'");
            dsUserTables.Select("A.TABLE_NAME");
            dsUserTables.Select("A.ID");
            dvUserTables = DBHelper.Instance.GetDataTable(dsUserTables.GetSql(), null);
        }
        else
        {
            DbSelect dsUserTables = new DbSelect("SYSOBJECTS A", "A", null);
            dsUserTables.IsInitDefaultValue = false;
            dsUserTables.Select("A.NAME", "TABLE_NAME");
            dsUserTables.Where("A.TYPE='U'");
            dvUserTables = DBHelper.Instance.GetDataTable(dsUserTables.GetSql(), null);
        }
        if (dvUserTables != null && dvUserTables.Rows.Count > 0)
        {
            for (int i = 0; i < dvUserTables.Rows.Count; i++)
            {
                var tableName = dvUserTables.Rows[i]["TABLE_NAME"].ToString();
                var enity = await base.Query(x => x.TableCode == tableName);

                TableManager.InitTableAndField(tableName, enity.FirstOrDefault()?.ID);
                Console.WriteLine($"{i}/{dvUserTables.Rows.Count}");
            }
        }
        #endregion

        #region 初始化所有视图
        DataTable dvUserViews = null;
        if (DBHelper.MySql)
        {
            //DbSelect dsUserViews = new DbSelect("USER_VIEWS A", "A", null);
            //dsUserViews.IsInitDefaultValue = false;
            //dsUserViews.Select("A.VIEW_NAME");
            string dvSql = "SHOW TABLE STATUS WHERE COMMENT='VIEW'";
            dvUserViews = DBHelper.Instance.GetDataTable(dvSql, null);
        }
        else
        {
            DbSelect dsUserTables = new DbSelect("SYSOBJECTS A", "A", null);
            dsUserTables.IsInitDefaultValue = false;
            dsUserTables.Select("A.NAME", "VIEW_NAME");
            dsUserTables.Where("A.TYPE='V'");
            dvUserViews = DBHelper.Instance.GetDataTable(dsUserTables.GetSql(), null);
        }
        if (dvUserViews != null && dvUserViews.Rows.Count > 0)
        {
            for (int i = 0; i < dvUserViews.Rows.Count; i++)
            {
                TableManager.InitTableAndField(dvUserViews.Rows[i]["VIEW_NAME"].ToString().ToUpper(), null);
            }
        }
        #endregion

        #region 删除所有不存在的表和视图
        int count = -1;
        DbSelect dsUserTables1 = null;
        //DbDelete ddTableCatalog = null;
        DbSelect dsTableCatalog = new DbSelect("SmTableCatalog A", "A");
        dsTableCatalog.Select("A.*");
        DataTable dtTableCatalog = DBHelper.Instance.GetDataTable(dsTableCatalog.GetSql());
        for (int i = 0; i < dtTableCatalog.Rows.Count; i++)
        {
            if (dtTableCatalog.Rows[i]["TypeCode"].ToString() == "TABLE")
            {
                if (DBHelper.MySql)
                {
                    dsUserTables1 = new DbSelect("INFORMATION_SCHEMA.TABLES A", "A", null);
                    dsUserTables1.IsInitDefaultValue = false;
                    string dbName = DBServerProvider.GetConnectionString().Split("Database=")[1].Split(";")[0]?.Trim();
                    dsUserTables1.Where($"A.TABLE_SCHEMA='{dbName}' AND A.TABLE_TYPE='BASE TABLE'");
                    dsUserTables1.Select("COUNT(*)");
                    count = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(dsUserTables1.GetSql()));
                }
                else
                {
                    dsUserTables1 = new DbSelect("SYSOBJECTS A", "A", null);
                    dsUserTables1.IsInitDefaultValue = false;
                    dsUserTables1.Select("COUNT(*)");
                    dsUserTables1.Where("A.TYPE='U'");
                    dsUserTables1.Where("A.NAME", "=", dtTableCatalog.Rows[i]["TableCode"].ToString());
                    count = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(dsUserTables1.GetSql()));
                }
            }
            else if (dtTableCatalog.Rows[i]["TypeCode"].ToString() == "VIEW")
            {
                if (DBHelper.MySql)
                {
                    dsUserTables1 = new DbSelect("USER_VIEWS A", "A", null);
                    dsUserTables1.IsInitDefaultValue = false;
                    dsUserTables1.Select("COUNT(*)");
                    dsUserTables1.Where("A.VIEW_NAME", "=", dtTableCatalog.Rows[i]["TableCode"].ToString());
                    count = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(dsUserTables1.GetSql()));
                }
                else
                {
                    dsUserTables1 = new DbSelect("SYSOBJECTS A", "A", null);
                    dsUserTables1.IsInitDefaultValue = false;
                    dsUserTables1.Select("COUNT(*)");
                    dsUserTables1.Where("A.TYPE='V'");
                    dsUserTables1.Where("A.NAME", "=", dtTableCatalog.Rows[i]["TableCode"].ToString());
                    count = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(dsUserTables1.GetSql()));
                }
            }
            if (count == 0)
            {
                string sql = string.Empty;
                sql = "DELETE A FROM SmFieldCatalog A WHERE A.TableCode='" + dtTableCatalog.Rows[i]["TableCode"] + "' AND A.IsActive='false'";
                DBHelper.Instance.ExcuteNonQuery(sql);

                sql = $"DELETE A FROM SmTableCatalog A WHERE A.ID='{dtTableCatalog.Rows[i]["ID"]?.ToString()}'";
                DBHelper.Instance.ExcuteNonQuery(sql);
            }
        }
        #endregion
        //});


        return Success(ResponseText.EXECUTE_SUCCESS);

    }
    #endregion
}