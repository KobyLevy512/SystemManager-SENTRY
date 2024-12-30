
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace SystemManager.Features
{
    internal class Performance
    {
        PerformanceCounter cpuCounter;
        PerformanceCounter cpuCounterProcess;
        PerformanceCounter diskPerformance;
        PerformanceCounter diskWritePerformance;
        PerformanceCounter diskPerformanceProcess;
        PerformanceCounter diskWritePerformanceProcess;

        public Performance()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounterProcess = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            diskPerformance = new PerformanceCounter("LogicalDisk", "Disk Reads/sec", "_Total");
            diskWritePerformance = new PerformanceCounter("LogicalDisk", "Disk Writes/sec", "_Total");
            diskPerformanceProcess = new PerformanceCounter("LogicalDisk", "Disk Reads/sec", Process.GetCurrentProcess().ProcessName);
            diskWritePerformanceProcess = new PerformanceCounter("LogicalDisk", "Disk Writes/sec", Process.GetCurrentProcess().ProcessName);
        }

        float Measure(uint iterations, PerformanceCounter cpuCounter)
        {
            float usage = 0;
            uint div = iterations;

            //Measure the use.
            while (iterations > 0)
            {
                usage += cpuCounter.NextValue();
                iterations--;
            }

            return usage / div;
        }

        public float GetProcessorUsage(bool pc)
        {
            float processorCount = (float)Environment.ProcessorCount * 2.0f;
            PerformanceCounter cpuCounter;

            //Check if to measure the all pc or just this procss.
            if (pc)
            {
                cpuCounter = this.cpuCounter;
                return Measure(1, cpuCounter) / processorCount;
            }
            else
            {
                cpuCounter = cpuCounterProcess;
                float res = Measure(1, cpuCounter) / processorCount;
                return res / processorCount;
            }
        }
        public float GetMemoryUsage(bool pc, uint iterations = 1)
        {
            long used = GC.GetTotalMemory(false);
            ComputerInfo p = new ComputerInfo();
            ulong availble = p.AvailablePhysicalMemory;
            ulong total = p.TotalPhysicalMemory;
            if(pc)
            {
                double res = (double)availble / (double)total * 100.0;
                return (float)res;
            }
            return (float)((double)used / (double)total * 100.0);
        }
        public float GetDiskReadUsage(bool pc, uint iterations = 1)
        {
            PerformanceCounter cpuCounter;

            //Check if to measure the all pc or just this procss.
            if (pc)
            {
                cpuCounter = this.diskPerformance;
            }
            else
            {
                //cpuCounter = diskPerformanceProcess;
                cpuCounter = this.diskPerformance;
            }
            return Measure(iterations, cpuCounter);
        }
        public float GetDiskWriteUsage(bool pc, uint iterations = 1)
        {
            PerformanceCounter cpuCounter;

            //Check if to measure the all pc or just this procss.
            if (pc)
            {
                cpuCounter = this.diskWritePerformance;
            }
            else
            {
                //cpuCounter = diskWritePerformanceProcess;
                cpuCounter = this.diskWritePerformance;
            }

            return Measure(1, cpuCounter);
        }
    }
}
