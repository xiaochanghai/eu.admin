namespace EU.Core.Model;

public class RootEntityTkey<Tkey> where Tkey : IEquatable<Tkey>
{
    private Tkey _id;

    /// <summary>
    /// ID
    /// 泛型主键Tkey
    /// </summary>
    [SugarColumn(IsNullable = false, IsPrimaryKey = true), Key]
    public Tkey ID { get; set; }
    //public Tkey ID
    //{
    //    get
    //    {
    //        if (typeof(Tkey) == typeof(Guid))
    //        {
    //            if (_id.ToString() == Guid.Empty.ToString())
    //            {
    //                _id = (Tkey)Convert.ChangeType(Guid.NewGuid(), typeof(Tkey));
    //            }
    //        }
    //        return _id;
    //    }
    //    set
    //    {
    //        _id = value;
    //    }
    //}
}