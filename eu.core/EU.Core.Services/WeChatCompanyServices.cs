using EU.Core.Common;
using EU.Core.Common.Helper;
using EU.Core.IRepository.Base;
using EU.Core.IServices;
using EU.Core.Model;
using EU.Core.Model.Models;
using EU.Core.Model.ViewModels;
using EU.Core.Services.BASE;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EU.Core.Repository.UnitOfWorks;

namespace EU.Core.Services
{
    /// <summary>
	/// WeChatCompanyServices
	/// </summary>
    public class WeChatCompanyServices : BaseServices<WeChatCompany>, IWeChatCompanyServices
    {
        readonly IUnitOfWorkManage _unitOfWorkManage;
        readonly ILogger<WeChatCompanyServices> _logger;
        public WeChatCompanyServices(IUnitOfWorkManage unitOfWorkManage, ILogger<WeChatCompanyServices> logger)
        {
            this._unitOfWorkManage = unitOfWorkManage;
            this._logger = logger;
        }  
        
    }
}