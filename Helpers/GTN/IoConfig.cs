using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers.GTN
{
    //public class IoConfig
    //{
    //    public string DisplayName { get; set; }

    //    public int Index { get; set; }

    //    public bool InvertSignal { get; set; }
    //}
    /// <summary>
    /// 构造函数
    /// </summary>
    public class IoConfig
    {
        /// <summary>EtherCAT从站号，你的全部DI在从站0</summary>
        public ushort Slave { get; set; }
        /// <summary>IO字节偏移地址</summary>
        public ushort ByteOffset { get; set; }
        /// <summary>当前字节内的bit位 0~7</summary>
        public ushort BitIndex { get; set; }
        /// <summary>IO名称备注</summary>
        public string Name { get; set; }

        public IoConfig(ushort slave, ushort byteOffset, ushort bitIndex, string name)
        {
            Slave = slave;
            ByteOffset = byteOffset;
            BitIndex = bitIndex;
            Name = name;
        }
    }
}