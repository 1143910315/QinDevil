using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace QinDevilCommon.SystemLay {
    public class SystemInfo {
        //获取CPU序列号
        public static string GetCpuID() {
            try {
                string cpuInfo = "";//cpu序列号 
                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc) {
                    if (cpuInfo.Length == 0) {
                        cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
                    }
                }
                if (cpuInfo.Length != 0) {
                    return cpuInfo;
                }
            } catch (Exception) {
            }
            return "unknow";
        }
        //获取网卡硬件地址 
        public static string GetMacAddress() {
            try {
                string mac = "";
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc) {
                    if ((bool)mo["IPEnabled"] == true) {
                        if (mac.Length == 0) {
                            mac = mo["MacAddress"].ToString();
                        }
                    }
                }
                if (mac.Length != 0) {
                    return mac;
                }
            } catch (Exception) {
            }
            return "unknow";
        }
        /*
        //3.获取硬盘ID 
        string GetDiskID() {
            try {
                String HDid = "";
                ManagementClass mc = new ManagementClass("Win32_DiskDrive");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc) {
                    HDid = (string)mo.Properties["Model"].Value;
                }
                moc = null;
                mc = null;
                return HDid;
            } catch {
                return "unknow";
            } finally {
            }
        }
        //4.获取IP地址
        string GetIPAddress() {
            try {
                string st = "";
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc) {
                    if ((bool)mo["IPEnabled"] == true) {
                        //st=mo["IpAddress"].ToString(); 
                        Array ar;
                        ar = (Array)(mo.Properties["IpAddress"].Value);
                        st = ar.GetValue(0).ToString();
                        break;
                    }
                }
                moc = null;
                mc = null;
                return st;
            } catch {
                return "unknow";
            } finally {
            }
        }
        /// 5.操作系统的登录用户名 
        string GetUserName() {
            try {
                string un = "";
                un = Environment.UserName;
                return un;
            } catch {
                return "unknow";
            } finally {
            }
        }
        //6.获取计算机名
        string GetComputerName() {
            try {
                return System.Environment.MachineName;
            } catch {
                return "unknow";
            } finally {
            }
        }
        ///7 PC类型 
        string GetSystemType() {
            try {
                string st = "";
                ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc) {

                    st = mo["SystemType"].ToString();

                }
                moc = null;
                mc = null;
                return st;
            } catch {
                return "unknow";
            } finally {
            }
        } 
                   catch 
            { 
                return "unknow"; 
            } 
            finally 
            { 
            } 
        }
    /// 8.物理内存 

    string GetTotalPhysicalMemory() {
        try {

            string st = "";
            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc) {

                st = mo["TotalPhysicalMemory"].ToString();

            }
            moc = null;
            mc = null;
            return st;
        }
        */
    }
}
