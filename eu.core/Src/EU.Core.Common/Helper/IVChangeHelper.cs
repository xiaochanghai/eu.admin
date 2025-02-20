using EU.Core.Model.Models;
using SqlSugar;
using System.Data;
using static EU.Core.Model.Consts;

namespace EU.Core.Common.Helper;

public class IVChangeHelper
{
    /// <summary>
    /// 处理库存
    /// </summary>
    /// <param name="Db">Db</param>
    /// <param name="materialId">物料ID</param>
    /// <param name="stockId">仓库ID</param>
    /// <param name="locationId">货位ID</param>
    /// <param name="qty">数量</param>
    /// <param name="changeType">库存类型</param>
    /// <param name="orderId">订单ID</param>
    /// <param name="orderDetailId">订单明细ID</param>
    /// <param name="batchNo">批次</param>
    /// <param name="remark">备注</param>
    /// <returns></returns>
    public static async Task Add(ISqlSugarClient Db, Guid? materialId, Guid? stockId, Guid? locationId, decimal? qty, ChangeType changeType, Guid? orderId, Guid? orderDetailId, string batchNo = null, string remark = null)
    {
        var inventory = await Db.Queryable<BdMaterialInventory>()
            .Where(x => x.StockId == stockId &&
            x.GoodsLocationId == locationId &&
            x.MaterialId == materialId)
            .WhereIF(batchNo.IsNotEmptyOrNull(), x => x.BatchNo == batchNo)
            .WhereIF(batchNo.IsNullOrEmpty(), x => (x.BatchNo == null || x.BatchNo == ""))
            .FirstAsync();
        if (batchNo.IsNullOrEmpty()) batchNo = null;
        if (inventory is null)
        {
            BdMaterialInventory inventoryInsert = new()
            {
                MaterialId = materialId,
                StockId = stockId,
                GoodsLocationId = locationId,
                BatchNo = batchNo,
                QTY = 0
            };
            await Db.Insertable(inventoryInsert).ExecuteCommandAsync();

            inventory = await Db.Queryable<BdMaterialInventory>()
                .Where(x => x.StockId == stockId &&
                x.GoodsLocationId == locationId &&
                x.MaterialId == materialId)
                .WhereIF(batchNo.IsNotEmptyOrNull(), x => x.BatchNo == batchNo)
                .WhereIF(batchNo.IsNullOrEmpty(), x => (x.BatchNo == null || x.BatchNo == ""))
                .FirstAsync();
        }

        BdMaterialIVChange change = new()
        {
            MaterialId = materialId,
            StockId = stockId,
            GoodsLocationId = locationId,
            BatchNo = batchNo,
            QTY = qty,
            ChangeType = changeType.ToString(),
            OrderId = orderId,
            OrderDetailId = orderDetailId,
            Remark = remark,
            BeforeQTY = inventory.QTY
        };


        switch (changeType)
        {
            case ChangeType.InventoryCheckIn:
            case ChangeType.InventoryIn:
                inventory.QTY += qty;
                break;
            case ChangeType.InventoryOut:
                if ((inventory.QTY - qty) < 0)
                {
                    var material = await Db.Queryable<BdMaterial>().FirstAsync(x => x.ID == materialId);
                    throw new Exception($"【{material.MaterialNames}】库存不足，剩余库存：{inventory.QTY.RemoveZero()}");
                }

                inventory.QTY -= qty;
                break;
            case ChangeType.InventoryCheckOut:
                inventory.QTY -= qty;
                break;
                //default:
                //    {
                //        return ExpressionType.Equal;
                //    }
        }

        change.AfterQTY = inventory.QTY;
        await Db.Updateable<BdMaterialInventory>()
            .SetColumns(it => new BdMaterialInventory()
            {
                QTY = inventory.QTY
            }, true)
            .Where(it => it.ID == inventory.ID)
            .ExecuteCommandAsync();
        await Db.Insertable(change).ExecuteCommandAsync();
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

    /// <summary>
    /// 库存变更类型
    /// </summary>
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
        /// 库存盘点出库
        /// </summary>
        InventoryCheckOut,
        /// <summary>
        /// 库存盘点入库
        /// </summary>
        InventoryCheckIn,
        /// <summary>
        /// 库存入库单
        /// </summary>
        InventoryIn,
        /// <summary>
        /// 库存出库单
        /// </summary>
        InventoryOut,
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
                        SET A.SerialNumber = C.NUM,
                            A.UpdateTime = getdate (),
                            A.UpdateBy = '{App.User.ID}',
                            A.ModificationNum = ISNULL (A.ModificationNum, 0) + 1
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
