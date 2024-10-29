/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* MfMould.cs
*
*功 能： N / A
* 类 名： MfMould
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:30:06  SimonHsiao   初版
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
	/// MfMould (服务)
	/// </summary>
    public class MfMouldServices : BaseServices<MfMould, MfMouldDto, InsertMfMouldInput, EditMfMouldInput>, IMfMouldServices
    {
        private readonly IBaseRepository<MfMould> _dal;
        public MfMouldServices(IBaseRepository<MfMould> dal)
        {
            this._dal = dal;
            base.BaseDal = dal;
        }
    }
}