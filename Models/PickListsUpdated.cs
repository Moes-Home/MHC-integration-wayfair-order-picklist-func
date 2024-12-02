using System;
using System.Collections.Generic;

namespace wayfair_order_picklist_dev.Models
{
    public class PickListsUpdated
    {
        public int Absoluteentry { get; set; }
        public string Name { get; set; }
        public string ObjectType { get; set; }
        public int OwnerCode { get; set; }
        public string PickDate { get; set; }
        public List<PickListsLine> PickListsLines { get; set; }
        public string Status { get; set; }
        public string UseBaseUnits { get; set; }
    }
}