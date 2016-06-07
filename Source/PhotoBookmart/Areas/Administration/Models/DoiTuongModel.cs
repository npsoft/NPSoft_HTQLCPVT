using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Products;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class BienDong_DuyetModel
    {
        public long Id { get; set; }

        public BienDong_DuyetModel() { }
    }

    public class BienDong_CatChetModel
    {
        public long Id { get; set; }
        public DateTime NgayBienDong { get; set; }

        public BienDong_CatChetModel() { }
    }

    public class BienDong_DungTroCapModel
    {
        public long Id { get; set; }
        public DateTime NgayBienDong { get; set; }

        public BienDong_DungTroCapModel() { }
    }

    public class BienDong_ChuyenDiaBanModel
    {
        public long Id { get; set; }
        public string MaHC { get; set; }
        public Guid? IDDiaChi { get; set; }
        public DateTime NgayBienDong { get; set; }

        public BienDong_ChuyenDiaBanModel() { }
    }

    public class BienDong_ThayDoiLoaiDoiTuong
    {
        public long Id { get; set; }
        public string MaLDT { get; set; }
        public decimal? MucTC { get; set; }
        public DateTime NgayBienDong { get; set; }
        public List<DoiTuong_LoaiDoiTuong_CT> MaLDT_Details { get; set; }

        public BienDong_ThayDoiLoaiDoiTuong()
        {
            MaLDT_Details = new List<DoiTuong_LoaiDoiTuong_CT>();
        }
    }
}
