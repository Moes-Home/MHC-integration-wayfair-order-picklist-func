using System.Collections.Generic;

namespace wayfair_order_picklist_dev.Models
{
    public class PickListsLine
    {
        public int AbsoluteEntry { get; set; }
        public int BaseObjectType { get; set; }
        public List<DocumentLinesBinAllocation> DocumentLinesBinAllocations { get; set; }
        public int LineNumber { get; set; }
        public int OrderEntry { get; set; }
        public int OrderRowID { get; set; }
        public string PickStatus { get; set; }
        public decimal PickedQuantity { get; set; }
        public decimal PreviouslyReleasedQuantity { get; set; }
        public decimal ReleasedQuantity { get; set; }
        // public List<SerialNumber> SerialNumbers { get; set; }
    }
}