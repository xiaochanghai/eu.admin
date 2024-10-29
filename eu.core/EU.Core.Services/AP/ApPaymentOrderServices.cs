/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ApPaymentOrder.cs
*
*功 能： N / A
* 类 名： ApPaymentOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/26 17:49:20  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 

using EU.Core.IServices;
using EU.Core.Model.Models;
using EU.Core.Services.BASE;
using EU.Core.IRepository.Base;

namespace EU.Core.Services
{
	/// <summary>
	/// 采购付款单 (服务)
	/// </summary>
    public class ApPaymentOrderServices : BaseServices<ApPaymentOrder, ApPaymentOrderDto, InsertApPaymentOrderInput, EditApPaymentOrderInput>, IApPaymentOrderServices
    {
        private readonly IBaseRepository<ApPaymentOrder> _dal;
        public ApPaymentOrderServices(IBaseRepository<ApPaymentOrder> dal)
        {
            this._dal = dal;
            base.BaseDal = dal;
        }
    }
}