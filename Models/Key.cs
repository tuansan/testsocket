using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetSockets.Models
{
    public class Key
    {
        public string Id { get; set; }
        public string TargetId { get; set; }
        public string Name { get; set; }
        public string Local { get; set; }
        public int Role { get; set; }
        public int Status { get; set; }
        public List<string> TaiXes { get; set; }
    }

    public enum ENVaiTro
    {
        Khach = 1, TaiXe = 2
    }

    public enum ENTrangThaiTaiXe
    {
        RANH, DANG_XACNHAN, DANG_CHAY
    }

    public enum ENTrangThaiKhach
    {
        RANH, DANG_YEUCAU, DANG_CHAY
    }
}
