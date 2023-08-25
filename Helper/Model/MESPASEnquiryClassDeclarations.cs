using System;
using System.Collections.Generic;
using System.Text;

namespace Helper.Model
{
   public class MESPASEnquiryClassDeclarations
    {
        public class Enquirydata
        {
            public string sourceType { get; set; }
            public string owner { get; set; }
            public string eventType { get; set; }
            public string entityId { get; set; }
            public long eventId { get; set; }
            public string status { get; set; }
        }

        public class Message
        {
            public string result { get; set; }
        }
   
       public class Enquiryheader
        {
            public long eventId { get; set; }
            public string enquiryDate { get; set; }
            public string owner { get; set; }
            public string status { get; set; }
            public string rfqUrl { get; set; }
            public string shipName { get; set; }
            public string enqrefNo { get; set; }
            public string maker { get; set; }
            public string type { get; set; }
            public string equipment { get; set; }
            public string serialNo { get; set; }
            public string docPath { get; set; }
            public string mappingPort { get; set; }
            public string port { get; set; }
            public string emailReceivedat { get; set; }
            public string emailProcessedat { get; set; }
            public string sourceType { get; set; }
            public int IsUpdatedMakerWithML { get; set; }
            public int IsUpdatedEquipmentWithML { get; set; }
            public List<EnquiryHeaderDocDetails> docHdrDetails { get; set; }
            public List<EnquiryDetails> itemDetails { get; set; }
        }
        public class EnquiryDetails
        {
            public string name { get; set; }
            public string remark { get; set; }
            public string partCode { get; set; }
            public string partName { get; set; }
            public string quantity { get; set; }
            public string unit { get; set; }
            public string cost { get; set; }
            public string status { get; set; }
            public string seqNo { get; set; }
            public string accountNo { get; set; }
            public string IsUpdatedMESPASItemsWithML { get; set; }
            public Component component { get; set; }
            public List<EnquiryDetailDocDetails> docdtlDetails { get; set; }
        }
        public class EnquiryHeaderDocDetails
        {
            public long docId { get; set; }
            public string fileId { get; set; }
            public string fileName { get; set; }
            public long enquiryHdrId { get; set; }
            public string docPath { get; set; }
            public string errorDescription { get; set; }
            public int createdBy { get; set; }
            public DateTime? createdAt { get; set; }
            public int isActive { get; set; }
        }
        public class EnquiryDetailDocDetails
        {
            public long docId { get; set; }
            public long enquiryDtlId { get; set; }
            public string docPath { get; set; }
            public string errorDescription { get; set; }
            public int createdBy { get; set; }
            public DateTime? createdAt { get; set; }
            public int isActive { get; set; }
        }
        public class Component
        {
            public string name { get; set; }
            public string execution { get; set; }
            public List<SpecificationDetails> specificationDetails { get; set; }
        }
        public class SpecificationDetails
        {
            public string name { get; set; }
            public string value { get; set; }
        }
        public class MSDItemData
        {
            public int msdItem_Id { get; set; }
            public string itemName { get; set; }
        }
        public class SNQItemData
        {
            public int snqItem_Id { get; set; }
            public string itemName { get; set; }
        }
        public class MakerData
        {
            public int maker_Id { get; set; }
            public string makerName { get; set; }
            public string makerCode { get; set; }

        }
        public class EquipmentData
        {
            public int equipment_Id { get; set; }
            public string equipmentName { get; set; }
            public string equipmentCode { get; set; }
        }
    }
}
