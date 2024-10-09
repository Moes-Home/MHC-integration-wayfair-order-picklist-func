using System;
using System.Collections.Generic;

namespace wayfair_order_picklist_dev.Models
{
    public class CrudData
    {
        public string Comment { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string DBName { get; set; }
        public Dictionary<string, object> FieldsAndValuesJson { get; set; }
        public string OperationStatus { get; set; }
        public string OperationType { get; set; }
        public string PrimaryFieldName { get; set; }
        public string PrimaryFieldValue { get; set; }
        public string SchemaName { get; set; }
        public string SequentialPrimaryKey { get; set; }
        public string TableName { get; set; }
    }

}
