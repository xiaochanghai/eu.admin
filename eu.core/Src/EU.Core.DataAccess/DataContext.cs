using System.Threading.Tasks;
using System.Threading;
using System;
using EU.Core.Model.Models;
using Microsoft.EntityFrameworkCore;
using EU.Core.Model.Models.RootTkey;
using EU.Core.Common.Helper;

namespace EU.Core.DataAccess;

public class DataContext : DbContext
{

    #region 基础档案
    public virtual DbSet<BdMaterialType> BdMaterialType { get; set; }
    public virtual DbSet<BdStock> BdStock { get; set; }
    public virtual DbSet<BdGoodsLocation> BdGoodsLocation { get; set; }
    public virtual DbSet<BdMaterial> BdMaterial { get; set; }
    public virtual DbSet<BdSettlementWay> BdSettlementWay { get; set; }
    public virtual DbSet<BdCustomer> BdCustomer { get; set; }
    public virtual DbSet<BdCustomerDeliveryAddress> BdCustomerDeliveryAddress { get; set; }
    public virtual DbSet<BdSupplier> BdSupplier { get; set; }

    /// <summary>
    /// 物料库存
    /// </summary>
    public virtual DbSet<BdMaterialInventory> BdMaterialInventory { get; set; }

    #endregion

    #region 销售管理

    /// <summary>
    /// 销售单
    /// </summary>
    public virtual DbSet<SdOrder> SdOrder { get; set; }
    /// <summary>
    /// 销售单明细
    /// </summary>
    public virtual DbSet<SdOrderDetail> SdOrderDetail { get; set; }

    public virtual DbSet<SdShipOrder> SdShipOrder { get; set; }
    public virtual DbSet<SdShipOrderDetail> SdShipOrderDetail { get; set; }
    public virtual DbSet<SdOutOrder> SdOutOrder { get; set; }
    public virtual DbSet<SdOutOrderDetail> SdOutOrderDetail { get; set; }
    public virtual DbSet<SdReturnOrder> SdReturnOrder { get; set; }
    public virtual DbSet<SdReturnOrderDetail> SdReturnOrderDetail { get; set; }


    #endregion

    #region 采购管理

    /// <summary>
    /// 请购单
    /// </summary>
    public virtual DbSet<PoRequestion> PoRequestion { get; set; }

    /// <summary>
    /// 请购单明细
    /// </summary>
    public virtual DbSet<PoRequestionDetail> PoRequestionDetail { get; set; }

    /// <summary>
    /// 采购单
    /// </summary>
    public virtual DbSet<PoOrder> PoOrder { get; set; }

    /// <summary>
    /// 采购单明细
    /// </summary>
    public virtual DbSet<PoOrderDetail> PoOrderDetail { get; set; }

    /// <summary>
    /// 采购单预付账款
    /// </summary>
    public virtual DbSet<PoOrderPrepayment> PoOrderPrepayment { get; set; }

    /// <summary>
    /// 采购到货通知单
    /// </summary>
    public virtual DbSet<PoArrivalOrder> PoArrivalOrder { get; set; }
    public virtual DbSet<PoArrivalOrderDetail> PoArrivalOrderDetail { get; set; }
    public virtual DbSet<PoInOrder> PoInOrder { get; set; }
    public virtual DbSet<PoInOrderDetail> PoInOrderDetail { get; set; }
    public virtual DbSet<PoReturnOrder> PoReturnOrder { get; set; }
    public virtual DbSet<PoReturnOrderDetail> PoReturnOrderDetail { get; set; }

    /// <summary>
    /// 采购扣款单明细
    /// </summary>
    public virtual DbSet<PoDeductionDetail> PoDeductionDetail { get; set; }

    /// <summary>
    /// 采购费用单明细
    /// </summary>
    public virtual DbSet<PoFeeDetail> PoFeeDetail { get; set; }
    #endregion

    #region 应付管理

    /// <summary>
    /// 应付期初建账
    /// </summary>
    public virtual DbSet<ApInitAccountOrder> ApInitAccountOrder { get; set; }

    /// <summary>
    /// 应付期初建账明细
    /// </summary>
    public virtual DbSet<ApInitAccountDetail> ApInitAccountDetail { get; set; }

    /// <summary>
    /// 应付对账单
    /// </summary>
    public virtual DbSet<ApCheckOrder> ApCheckOrder { get; set; }

    /// <summary>
    /// 应付对账单明细
    /// </summary>
    public virtual DbSet<ApCheckDetail> ApCheckDetail { get; set; }

    /// <summary>
    /// 应付发票单
    /// </summary>
    public virtual DbSet<ApInvoiceOrder> ApInvoiceOrder { get; set; }

    /// <summary>
    /// 应付对应发票
    /// </summary>
    public virtual DbSet<ApInvoiceAssociation> ApInvoiceAssociation { get; set; }

    /// <summary>
    /// 应付发票单明细
    /// </summary>
    public virtual DbSet<ApInvoiceDetail> ApInvoiceDetail { get; set; }

    /// <summary>
    /// 采购付款单明细
    /// </summary>
    public virtual DbSet<ApPaymentDetail> ApPaymentDetail { get; set; }

    /// <summary>
    /// 采购付款单
    /// </summary>
    public virtual DbSet<ApPaymentOrder> ApPaymentOrder { get; set; }

    /// <summary>
    /// 采购付款核销明细
    /// </summary>
    public virtual DbSet<ApPaymentWriteOff> ApPaymentWriteOff { get; set; }

    /// <summary>
    /// 采购预付款
    /// </summary>
    public virtual DbSet<ApPrepaidOrder> ApPrepaidOrder { get; set; }

    /// <summary>
    /// 采购预付款明细
    /// </summary>
    public virtual DbSet<ApPrepaidDetail> ApPrepaidDetail { get; set; }

    #endregion

    #region 应收管理
    /// <summary>
    /// 应收对账单
    /// </summary>
    public virtual DbSet<ArCheckOrder> ArCheckOrder { get; set; }

    /// <summary>
    /// 应收对账单明细
    /// </summary>
    public virtual DbSet<ArCheckDetail> ArCheckDetail { get; set; }

    /// <summary>
    /// 应收期初建账
    /// </summary>
    public virtual DbSet<ArInitAccountOrder> ArInitAccountOrder { get; set; }

    /// <summary>
    /// 应收期初建账明细
    /// </summary>
    public virtual DbSet<ArInitAccountDetail> ArInitAccountDetail { get; set; }

    /// <summary>
    /// 应收开票
    /// </summary>
    public virtual DbSet<ArInvoiceOrder> ArInvoiceOrder { get; set; }

    /// <summary>
    /// 应收开票明细
    /// </summary>
    public virtual DbSet<ArInvoiceDetail> ArInvoiceDetail { get; set; }

    /// <summary>
    /// 应收开票对应发票
    /// </summary>
    public virtual DbSet<ArInvoiceAssociation> ArInvoiceAssociation { get; set; }

    /// <summary>
    /// 销售收款单
    /// </summary>
    public virtual DbSet<ArSalesCollectionOrder> ArSalesCollectionOrder { get; set; }

    /// <summary>
    /// 销售收款单明细
    /// </summary>
    public virtual DbSet<ArSalesCollectionDetail> ArSalesCollectionDetail { get; set; }

    /// <summary>
    /// 销售收款核销明细
    /// </summary>
    public virtual DbSet<ArSalesCollectionWriteOff> ArSalesCollectionWriteOff { get; set; }

    /// <summary>
    /// 销售预收款
    /// </summary>
    public virtual DbSet<ArPrepaidOrder> ArPrepaidOrder { get; set; }

    /// <summary>
    /// 销售预收款明细
    /// </summary>
    public virtual DbSet<ArPrepaidDetail> ArPrepaidDetail { get; set; }


    #endregion 

    #region 设备管理

    /// <summary>
    /// 设备分类
    /// </summary>
    public virtual DbSet<EmMachineType> EmMachineType { get; set; }

    /// <summary>
    /// 设备基础资料
    /// </summary>
    public virtual DbSet<EmMachine> EmMachine { get; set; }

    #endregion

    #region 工模治具
    /// <summary>
    /// 工模治具
    /// </summary>
    public virtual DbSet<MfMould> MfMould { get; set; }

    /// <summary>
    /// 工模治具类别
    /// </summary>
    public virtual DbSet<MfMouldType> MfMouldType { get; set; }

    /// <summary>
    /// 工模治具入账
    /// </summary>
    public virtual DbSet<MfInOrder> MfInOrder { get; set; }

    /// <summary>
    /// 工模治具入账明细
    /// </summary>
    public virtual DbSet<MfInOrderDetail> MfInOrderDetail { get; set; }
    #endregion

    #region 生产管理
    /// <summary>
    /// 生产工单
    /// </summary>
    public virtual DbSet<PdOrder> PdOrder { get; set; }

    /// <summary>
    /// 生产工单-材料明细
    /// </summary>
    public virtual DbSet<PdOrderMaterial> PdOrderMaterial { get; set; }

    /// <summary>
    /// 生产工单-对应订单
    /// </summary>
    public virtual DbSet<PdOrderDetail> PdOrderDetail { get; set; }

    /// <summary>
    /// 生产工单工艺路线
    /// </summary>
    public virtual DbSet<PdOrderProcess> PdOrderProcess { get; set; }

    /// <summary>
    /// 生产工单工模治具
    /// </summary>
    public virtual DbSet<PdOrderMould> PdOrderMould { get; set; }

    /// <summary>
    /// 生产计划工单
    /// </summary>
    public virtual DbSet<PdPlanOrder> PdPlanOrder { get; set; }

    /// <summary>
    /// 生产计划工单明细
    /// </summary>
    public virtual DbSet<PdPlanDetail> PdPlanDetail { get; set; }

    /// <summary>
    /// 需求分析工单
    /// </summary>
    public virtual DbSet<PdRequireOrder> PdRequireOrder { get; set; }

    /// <summary>
    /// 需求工单分析
    /// </summary>
    public virtual DbSet<PdRequireAnalysis> PdRequireAnalysis { get; set; }

    /// <summary>
    /// 材料补发工单
    /// </summary>
    public virtual DbSet<PdReissueOrder> PdReissueOrder { get; set; }

    /// <summary>
    /// 材料补发工单明细
    /// </summary>
    public virtual DbSet<PdReissueDetail> PdReissueDetail { get; set; }

    /// <summary>
    /// 材料出库工单
    /// </summary>
    public virtual DbSet<PdOutOrder> PdOutOrder { get; set; }

    /// <summary>
    /// 材料出库工单明细
    /// </summary>
    public virtual DbSet<PdOutDetail> PdOutDetail { get; set; }

    /// <summary>
    /// 材料退库工单
    /// </summary>
    public virtual DbSet<PdReturnOrder> PdReturnOrder { get; set; }

    /// <summary>
    /// 材料退库工单明细
    /// </summary>
    public virtual DbSet<PdReturnDetail> PdReturnDetail { get; set; }

    /// <summary>
    /// 材料完工工单
    /// </summary>
    public virtual DbSet<PdCompleteOrder> PdCompleteOrder { get; set; }

    /// <summary>
    /// 产品完工入库明细
    /// </summary>
    public virtual DbSet<PdCompleteDetail> PdCompleteDetail { get; set; }

    #endregion

    #region 产品结构

    /// <summary>
    /// 设备分类
    /// </summary>
    public virtual DbSet<PsWorkShop> PsWorkShop { get; set; }

    /// <summary>
    /// 工序
    /// </summary>
    public virtual DbSet<PsProcess> PsProcess { get; set; }

    /// <summary>
    /// 工序机台
    /// </summary>
    public virtual DbSet<PsProcessMachine> PsProcessMachine { get; set; }

    /// <summary>
    /// 工序外协厂商
    /// </summary>
    public virtual DbSet<PsProcessSupplier> PsProcessSupplier { get; set; }

    /// <summary>
    /// 工序单价
    /// </summary>
    public virtual DbSet<PsProcessPrice> PsProcessPrice { get; set; }

    /// <summary>
    /// 工序人员
    /// </summary>
    public virtual DbSet<PsProcessEmployee> PsProcessEmployee { get; set; }

    /// <summary>
    /// 工序不良原因
    /// </summary>
    public virtual DbSet<PsProcessBadReason> PsProcessBadReason { get; set; }

    /// <summary>
    /// 工序模板
    /// </summary>
    public virtual DbSet<PsProcessTemplate> PsProcessTemplate { get; set; }

    /// <summary>
    /// 工序模板明细
    /// </summary>
    public virtual DbSet<PsProcessTemplateDetail> PsProcessTemplateDetail { get; set; }

    /// <summary>
    /// 工序模板物料
    /// </summary>
    public virtual DbSet<PsProcessTemplateMaterial> PsProcessTemplateMaterial { get; set; }

    /// <summary>
    /// BOM
    /// </summary>
    public virtual DbSet<PsBOM> PsBOM { get; set; }

    /// <summary>
    /// BOM物料
    /// </summary>
    public virtual DbSet<PsBOMMaterial> PsBOMMaterial { get; set; }


    /// <summary>
    /// BOM工模治具
    /// </summary>
    public virtual DbSet<PsBOMMould> PsBOMMould { get; set; }

    /// <summary>
    /// BOM工艺路线
    /// </summary>
    public virtual DbSet<PsBOMProcess> PsBOMProcess { get; set; }

    #endregion

    #region 系统相关
    /// <summary>
    /// 系统模块
    /// </summary>
    public virtual DbSet<SmModules> SmModules { get; set; }
    public virtual DbSet<SmRoles> SmRoles { get; set; }
    public virtual DbSet<SmRoleModule> SmRoleModule { get; set; }

    public virtual DbSet<SmModuleSql> SmModuleSql { get; set; }
    public virtual DbSet<SmModuleColumn> SmModuleColumn { get; set; }
    public virtual DbSet<SmUsers> SmUsers { get; set; }
    public virtual DbSet<SmUserRole> SmUserRole { get; set; }
    public virtual DbSet<SmLov> SmLov { get; set; }
    public virtual DbSet<SmLovDetail> SmLovDetail { get; set; }
    public virtual DbSet<SmAutoCode> SmAutoCode { get; set; }
    public virtual DbSet<FileAttachment> FileAttachment { get; set; }
    public virtual DbSet<SmCompany> SmCompany { get; set; }
    public virtual DbSet<SmDepartment> SmDepartment { get; set; }
    /// <summary>
    /// 功能权限
    /// </summary>
    public virtual DbSet<SmFunctionPrivilege> SmFunctionPrivilege { get; set; }
    public virtual DbSet<SmRoleFunction> SmRoleFunction { get; set; }

    /// <summary>
    /// 系统参数分组
    /// </summary>
    public virtual DbSet<SmConfigGroup> SmConfigGroup { get; set; }

    /// <summary>
    /// 系统参数配置
    /// </summary>
    public virtual DbSet<SmConfig> SmConfig { get; set; }

    //public virtual DbSet<SmNode> SmNodes { get; set; }
    //public virtual DbSet<SmEdge> SmEdges { get; set; }
    //public virtual DbSet<SmProjectFlow> SmProjectFlow { get; set; }
    public virtual DbSet<SmEmployee> SmEmployee { get; set; }

    /// <summary>
    /// 系统表字典
    /// </summary>
    public virtual DbSet<SmTableCatalog> SmTableCatalog { get; set; }

    /// <summary>
    /// 系统表栏位
    /// </summary>
    public virtual DbSet<SmFieldCatalog> SmFieldCatalog { get; set; }

    /// <summary>
    /// 导入模板定义
    /// </summary>
    public virtual DbSet<SmImpTemplate> SmImpTemplate { get; set; }

    /// <summary>
    /// 导入模板定义明细
    /// </summary>
    public virtual DbSet<SmImpTemplateDetail> SmImpTemplateDetail { get; set; }

    /// <summary>
    /// 导入数据明细
    /// </summary>
    public virtual DbSet<SmImportDataDetail> SmImportDataDetail { get; set; }

    /// <summary>
    /// API接口授权
    /// </summary>
    //public virtual DbSet<SmApi> SmApi { get; set; }

    /// <summary>
    /// 省份
    /// </summary>
    public virtual DbSet<SmProvince> SmProvince { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    public virtual DbSet<SmCity> SmCity { get; set; }

    /// <summary>
    /// 区县
    /// </summary>
    public virtual DbSet<SmCounty> SmCounty { get; set; }

    /// <summary>
    /// 微信用户
    /// </summary>
    //public virtual DbSet<WxUser> WxUser { get; set; }

    /// <summary>
    //任务管理
    /// </summary>
    public virtual DbSet<SmQuartzJob> SmQuartzJob { get; set; }

    #endregion 

    //占位符

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentDateTime = DateTime.Now;
        foreach (var audiTableEntity in ChangeTracker.Entries<BaseEntity>())
        {
            if (audiTableEntity.State != EntityState.Added && audiTableEntity.State != EntityState.Modified) continue;
            switch (audiTableEntity.State)
            {
                case EntityState.Added:
                    audiTableEntity.Property("UpdateBy").IsModified = false;
                    audiTableEntity.Property("UpdateTime").IsModified = false;
                    audiTableEntity.Entity.CreatedTime = currentDateTime;
                    audiTableEntity.Entity.CreatedBy = Utility.GetUserId();
                    break;
                case EntityState.Modified:
                    audiTableEntity.Property("CreatedBy").IsModified = false;
                    audiTableEntity.Property("CreatedTime").IsModified = false;
                    audiTableEntity.Entity.UpdateTime = currentDateTime;
                    audiTableEntity.Entity.UpdateBy = Utility.GetUserId();
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);

    }

    public override int SaveChanges()
    {
        var currentDateTime = DateTime.Now;
        foreach (var audiTableEntity in ChangeTracker.Entries<BaseEntity>())
        {
            if (audiTableEntity.State != EntityState.Added && audiTableEntity.State != EntityState.Modified) continue;
            switch (audiTableEntity.State)
            {
                case EntityState.Added:
                    audiTableEntity.Property("UpdateBy").IsModified = false;
                    audiTableEntity.Property("UpdateTime").IsModified = false;
                    audiTableEntity.Entity.CreatedTime = currentDateTime;
                    audiTableEntity.Entity.CreatedBy = Utility.GetUserId();
                    break;
                case EntityState.Modified:
                    audiTableEntity.Property("CreatedBy").IsModified = false;
                    audiTableEntity.Property("CreatedTime").IsModified = false;
                    audiTableEntity.Entity.UpdateTime = currentDateTime;
                    audiTableEntity.Entity.UpdateBy = Utility.GetUserId();
                    break;
            }
        }
        return base.SaveChanges();

    }
}
