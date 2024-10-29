using System.Data;
using EU.Core.Common.Helper;
using Xunit;

namespace EU.Core.Tests.Common_Test
{
    public class DbAccess_Should
    {

        [Fact]
        public async void Test()
        {
            //AppSetting.Init();

            string sql = "SELECT * FROM Ghra_Grade";
            DataTable dt = await DBHelper.GetDataTableAsync(sql);

            //var list = await DBHelper.QueryListAsync<Ghra_Grade>(sql);

            //var entity = new Ghra_Grade();
            //DBHelper.Add(entity);

        }
         
    }
}
