using System;

namespace Common.DAL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignAttribute : Attribute
    {
        public string ForeignTable { get; set; }
        public string ForeignColumn { get; set; }

        public ForeignAttribute(string foreignTable, string foreignColumn = nameof(IEntity.ID))
        {
            ForeignTable = foreignTable;
            ForeignColumn = foreignColumn;
        }
    }
}
