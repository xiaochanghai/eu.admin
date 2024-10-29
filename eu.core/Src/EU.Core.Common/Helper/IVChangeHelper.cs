using System.Data;
using EU.Core.Model.Models;
using SqlSugar;

namespace EU.Core.Common.Helper;

public class IVChangeHelper
{
    public static void Add(BdMaterialIVChange change, IDbTransaction trans = null)
    {
        DbInsert di = new("BdMaterialIVChange");
        di.Values("MaterialId", change.MaterialId);
        di.Values("StockId", change.StockId);
        di.Values("GoodsLocationId", change.GoodsLocationId);
        di.Values("QTY", change.QTY);
        di.Values("BeforeQTY", change.BeforeQTY);
        di.Values("AfterQTY", change.AfterQTY);
        di.Values("ChangeType", change.ChangeType);
        di.Values("OrderId", change.OrderId);
        di.Values("OrderDetailId", change.OrderDetailId);
        DBHelper.ExecuteDML(di.GetSql(), null, null, trans);
    }

    /// <summary>
    /// 获取物料库存
    /// </summary>
    /// <param name="MaterialId">物料ID</param>
    /// <param name="StockId">仓库ID</param>
    /// <param name="GoodsLocationId">货位ID</param>
    /// <param name="trans">trans</param>
    /// <returns></returns>
    public static decimal? GetMaterialInventory(Guid? MaterialId, Guid? StockId, Guid? GoodsLocationId, IDbTransaction trans = null)
    {
        decimal? QTY = 0;
        string sql = @"SELECT *
                            FROM BdMaterialInventory
                            WHERE MaterialId = '{0}'
                                  AND StockId = '{1}'
                                  AND GoodsLocationId = '{2}'";
        sql = string.Format(sql, MaterialId, StockId, GoodsLocationId);
        var iv = DBHelper.Instance.QueryTransFirst<BdMaterialInventory>(sql, null, null, trans);
        if (iv != null)
            QTY = iv.QTY;
        else
        {
            DbInsert di = new("BdMaterialInventory");
            di.Values("MaterialId", MaterialId);
            di.Values("StockId", StockId);
            di.Values("GoodsLocationId", GoodsLocationId);
            di.Values("QTY", 0);
            DBHelper.ExecuteDML(di.GetSql(), null, null, trans);
        }

        return QTY;
    }

    public enum ChangeType
    {
        /// <summary>
        /// 销售出库
        /// </summary>
        SalesOut,
        /// <summary>
        /// 销售退货
        /// </summary>
        SalesReturn,
        /// <summary>
        /// 采购入库
        /// </summary>
        PurchaseIn,
        /// <summary>
        /// 采购退货
        /// </summary>
        PurchaseReturn,
        /// <summary>
        /// 库存调整
        /// </summary>
        InventoryAdjust,
        /// <summary>
        /// 库存调拨
        /// </summary>
        InventoryTransfers,
        /// <summary>
        /// 库存盘点
        /// </summary>
        InventoryCheck,
        /// <summary>
        /// 库存其他入库单
        /// </summary>
        InventoryOtherIn,
        /// <summary>
        /// 库存其他出库单
        /// </summary>
        InventoryOtherOut,
        /// <summary>
        /// 库存初始化
        /// </summary>
        InventoryInit
    }

    #region 修改税额
    /// <summary>
    /// 修改税额
    /// 比如税率13%情况下，客户按未税价计算，含税金额=单价x数量x1.13，未税金额=单价x数量
    /// 比如税率13 % 情况下，客户按含税价计算，含税金额 = 单价x数量，未税金额 = 单价x数量 / 1.13 
    /// </summary>
    /// <param name="taxType"></param>
    /// <param name="taxRate"></param>
    /// <param name="price"></param>
    /// <param name="qty"></param>
    /// <returns></returns>
    public static (decimal? NoTaxAmount, decimal? TaxAmount, decimal? TaxIncludedAmount) UpdataTaxAmount(string taxType, decimal? taxRate, decimal? price, decimal? qty)
    {
        decimal? NoTaxAmount = 0;
        decimal? TaxAmount = 0;
        decimal? TaxIncludedAmount = 0;
        if (taxType == "ZeroTax" || taxRate == 0)
        {
            NoTaxAmount = price * qty;
            TaxAmount = 0;
            TaxIncludedAmount = NoTaxAmount;
        }//未税
        else if (taxType == "ExcludingTax")
        {

            NoTaxAmount = price * qty;
            TaxIncludedAmount = NoTaxAmount * ((100 + taxRate) / 100);
            TaxAmount = TaxIncludedAmount - NoTaxAmount;
        }//含税
        else if (taxType == "IncludingTax")
        {
            TaxIncludedAmount = price * qty;
            NoTaxAmount = TaxIncludedAmount / ((100 + taxRate) / 100);
            TaxAmount = TaxIncludedAmount - NoTaxAmount;
        }
        return (NoTaxAmount, TaxAmount, TaxIncludedAmount);
    }
    #endregion

    #region 修改订单排序号
    /// <summary>
    /// 修改订单排序号
    /// </summary>
    /// <param name="Db">Db</param>
    /// <param name="tableName">表名</param>
    /// <param name="masterId">主表ID</param>
    /// <returns></returns>
    public static async Task UpdataOrderDetailSerialNumber(ISqlSugarClient Db, string tableName, Guid? masterId)
    {
        if (masterId != null)
            await UpdataOrderDetailSerialNumber(Db, tableName, masterId.Value);
    }

    /// <summary>
    /// 修改订单排序号
    /// </summary>
    /// <param name="Db">Db</param>
    /// <param name="tableName">表名</param>
    /// <param name="masterId">主表ID</param>
    /// <returns></returns>
    public static async Task UpdataOrderDetailSerialNumber(ISqlSugarClient Db, string tableName, Guid masterId)
    {
        string sql = @$"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM {tableName} A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM {tableName} A
                                          WHERE     1 = 1
                                                AND A.OrderId =
                                                    '{masterId}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID WHERE A.SerialNumber != C.NUM OR A.SerialNumber IS NULL";
        await Db.Ado.ExecuteCommandAsync(sql);
    }
    #endregion

}
