/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvTransfers.cs
*
*功 能： N / A
* 类 名： IvTransfers
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:02:03  SimonHsiao   初版
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
	/// 库存调拨单 (服务)
	/// </summary>
    public class IvTransfersServices : BaseServices<IvTransfers, IvTransfersDto, InsertIvTransfersInput, EditIvTransfersInput>, IIvTransfersServices
    {
        private readonly IBaseRepository<IvTransfers> _dal;
        public IvTransfersServices(IBaseRepository<IvTransfers> dal)
        {
            this._dal = dal;
            base.BaseDal = dal;
        }
    }
}