namespace EU.Core.Model;


/// <summary>
/// 全局常量
/// </summary>
public class Consts
{

    #region 销售管理
    /// <summary>
    /// 销售订单-订单状态
    /// </summary>
    public static class DIC_SALES_ORDER_STATUS
    {
        /// <summary>
        /// 待出货
        /// </summary>
        public const string WaitShip = "WaitShip";
        /// <summary>
        /// 出货中
        /// </summary>
        public const string InShip = "InShip";

        /// <summary>
        /// 出货完成
        /// </summary>
        public const string ShipComplete = "ShipComplete";

        /// <summary>
        /// 出库中
        /// </summary>
        public const string InOut = "InOut";

        /// <summary>
        ///  出库完成
        /// </summary>
        public const string OutComplete = "OutComplete";

        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";

    }

    /// <summary>
    /// 销售订单变更-订单状态
    /// </summary>
    public static class DIC_SALES_CHANGE_ORDER_STATUS
    {
        /// <summary>
        /// 未生效
        /// </summary>
        public const string Invalid = "Invalid";


        /// <summary>
        /// 已生效
        /// </summary>
        public const string Valid = "Valid";

    }

    /// <summary>
    /// 发货单-订单状态
    /// </summary>
    public static class DIC_SALES_SHIP_ORDER_STATUS
    {
        /// <summary>
        /// 待出货
        /// </summary>
        public const string WaitShip = "WaitShip";
        /// <summary>
        /// 出货中
        /// </summary>
        public const string InShip = "InShip";

        /// <summary>
        /// 出货完成
        /// </summary>
        public const string ShipComplete = "ShipComplete";
        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";

    }



    /// <summary>
    /// 出库单-订单状态
    /// </summary>
    public static class DIC_SALES_OUT_ORDER_STATUS
    {
        /// <summary>
        /// 待出库
        /// </summary>
        public const string WaitOut = "WaitOut";
        /// <summary>
        /// 出库完成
        /// </summary>
        public const string OutComplete = "OutComplete";
        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";
        /// <summary>
        ///  退库中
        /// </summary>
        public const string InReturn = "InReturn";
        /// <summary>
        ///  退库完成
        /// </summary>
        public const string ReturnComplete = "ReturnComplete";

    }

    /// <summary>
    /// 退库单-订单状态
    /// </summary>
    public static class DIC_SALES_RETURN_ORDER_STATUS
    {
        /// <summary>
        /// 待退回
        /// </summary>
        public const string WaitReturn = "WaitReturn";
        /// <summary>
        /// 已退回
        /// </summary>
        public const string HasReturn = "HasReturn";

    }

    /// <summary>
    /// 出库单-订单来源
    /// </summary>
    public static class DIC_SALES_OUT_ORDER_SOURCE
    {
        /// <summary>
        /// 出货单
        /// </summary>
        public const string Ship = "Ship";
        /// <summary>
        /// 销售单
        /// </summary>
        public const string Sales = "Sales";

    }
    #endregion

    #region 采购管理

    /// <summary>
    /// 请购单作业-订单状态
    /// </summary>
    public static class DIC_PURCHASE_REQUEST_STATUS
    {
        /// <summary>
        /// 待处理
        /// </summary>
        public const string Wait = "Wait";

        /// <summary>
        /// 采购中
        /// </summary>
        public const string InPurchase = "InPurchase";

        /// <summary>
        /// 采购完成
        /// </summary>
        public const string PurchaseComplete = "PurchaseComplete";
        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";
    }


    /// <summary>
    /// 采购单作业-订单状态
    /// </summary>
    public static class DIC_PURCHASE_ORDER_STATUS
    {
        /// <summary>
        /// 待处理
        /// </summary>
        public const string Wait = "Wait";
        /// <summary>
        /// 出货通知中
        /// </summary>
        public const string InNotice = "InNotice";

        /// <summary>
        /// 出货通知完成
        /// </summary>
        public const string NoticeComplete = "NoticeComplete";

        /// <summary>
        /// 入库中
        /// </summary>
        public const string In = "In";

        /// <summary>
        ///  入库完成
        /// </summary>
        public const string InComplete = "InComplete";

        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";
    }

    /// <summary>
    /// 采购到货通知-订单状态
    /// </summary>
    public static class DIC_PURCHASE_NOTICE_ORDER_STATUS
    {
        /// <summary>
        /// 待处理
        /// </summary>
        public const string Wait = "Wait";

        /// <summary>
        /// 入库中
        /// </summary>
        public const string In = "In";

        /// <summary>
        ///  入库完成
        /// </summary>
        public const string InComplete = "InComplete";

        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";
    }



    /// <summary>
    /// 采购入库单-订单状态
    /// </summary>
    public static class DIC_PURCHASE_IN_ORDER_STATUS
    {
        /// <summary>
        /// 待处理
        /// </summary>
        public const string Wait = "Wait";

        /// <summary>
        ///  完成入库
        /// </summary>
        public const string CompleteIn = "CompleteIn";

        /// <summary>
        ///  退库中
        /// </summary>
        public const string InReturn = "InReturn";

        /// <summary>
        ///  退库完成
        /// </summary>
        public const string CompleteReturn = "CompleteReturn";

        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";
    }

    /// <summary>
    /// 采购入库单-订单状态
    /// </summary>
    public static class DIC_PURCHASE_RETURN_ORDER_STATUS
    {
        /// <summary>
        /// 待处理
        /// </summary>
        public const string Wait = "Wait";

        /// <summary>
        ///  退库完成
        /// </summary>
        public const string CompleteReturn = "CompleteReturn";

        /// <summary>
        ///  订单完结
        /// </summary>
        public const string OrderComplete = "OrderComplete";
    }
    #endregion

    #region 系统

    /// <summary>
    /// 数据审核状态
    /// </summary>
    public static class DIC_SYSTEM_AUDIT_STATUS
    {
        /// <summary>
        /// 审核通过
        /// </summary>
        public const string CompleteAudit = "CompleteAudit";


        /// <summary>
        /// 草稿
        /// </summary>
        public const string Add = "Add";

    }
    #endregion
}