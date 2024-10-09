using System;

namespace wayfair_order_picklist_dev.Models
{
    public class OrderLine
    {
        public string ObjectType { get; set; }
        public DateTime PickDate { get; set; }
        public string BaseObjectType { get; set; }
        public string DocEntry { get; set; }
        public string LineNum { get; set; }
        public string ReleasedQuantity { get; set; }
        public string StagingtableId { get; set; }
        public string DBName { get; set; }
    }
}