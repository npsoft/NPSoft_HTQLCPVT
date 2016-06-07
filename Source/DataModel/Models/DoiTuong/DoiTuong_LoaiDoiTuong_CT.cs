using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Schema("DoiTuong")]
    [Alias("DoiTuong_LoaiDoiTuong_CT")]
    public partial class DoiTuong_LoaiDoiTuong_CT : BasicModelBase
    {
        public Guid CodeObj { get; set; }
        public string CodeType { get; set; }
        public bool IsDelete { get; set; }

        public string Type1_InfoFather { get; set; }
        public string Type1_InfoMother { get; set; }

        public string Type3_FullName { get; set; }
        public DateTime Type3_DateOfBirth { get; set; }
        public bool Type3_DateOfBirth_IsMonth { get; set; }
        public bool Type3_DateOfBirth_IsDate { get; set; }
        public string Type3_Gender { get; set; }
        public string Type3_CurrAddr { get; set; }
        public string Type3_StatusLearn { get; set; }

        public string Type4_MaritalStatus { get; set; }
        public string Type4_InfoAdditional { get; set; }

        public string Type5_SelfServing { get; set; }
        public string Type5_Carer { get; set; }

        public DoiTuong_LoaiDoiTuong_CT() { }
    }

    [Schema("Products")]
    public partial class OptionInProduct : ModelBase
    {
        [Default(0)]
        [ForeignKey(typeof(Product), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long ProductId { get; set; }
        [Ignore]
        public string Product_Name { get; set; }
        [Ignore]
        public string Option_Name { get; set; }

        [Default(0)]
        [ForeignKey(typeof(Product_Option), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long ProductOptionId { get; set; }

        /// <summary>
        /// If isRequire then can not remove
        /// </summary>
        public bool isRequire { get; set; }

        /// <summary>
        /// Default quantity the admin set this option for the product
        /// </summary>
        public int DefaultQuantity { get; set; }

        public int MaxQuantity { get; set; }

        public int MinQuantity { get; set; }

        public bool CanApplyCoupon { get; set; }

        public OptionInProduct()
        {
            CreatedOn = DateTime.Now;
        }
    }
}
