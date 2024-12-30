using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using SystemManager.Features;
using SystemManager.Features.DB;

namespace SystemManager
{
    public partial class MainMenu : Form
    {
        Timer performanceTimer, tablesTimer;
        public MainMenu()
        {
            InitializeComponent();
            StartPerformanceMeasure();
            StartTablesMeasure();
        }

        void StartPerformanceMeasure()
        {
            Performance performance = new Performance();
            SolidBrush green = new SolidBrush(Color.FromArgb(18, 165, 26));
            SolidBrush purple = new SolidBrush(Color.FromArgb(84, 66, 142));
            float cpuT = 0, cpu = 0;
            float memT = 0, mem = 0;
            float diskT = 0, disk = 0;
            float diskWriteT = 0, diskWrite = 0;
            performanceTimer = new Timer();
            performanceTimer.Interval = 2000;
            performanceTimer.Tick += (o, e) =>
            {
                Task.Run(() =>
                {
                    cpuT = performance.GetProcessorUsage(true) * 100.0f * 1.28f;
                    if (cpuT > 128.0f) cpuT = 128.0f;

                    cpu = performance.GetProcessorUsage(false) * 100.0f * 1.28f;
                    if (cpu > 128.0f) cpu = 128.0f;

                    mem = performance.GetMemoryUsage(false);
                    memT = performance.GetMemoryUsage(true);

                    //disk = performance.GetDiskReadUsage(false);
                    diskT = performance.GetDiskReadUsage(true);

                    //diskWrite = performance.GetDiskWriteUsage(false);
                    diskWriteT = performance.GetDiskWriteUsage(true);

                    processorCanvas.Invalidate();
                    memoryCanvas.Invalidate();
                    diskReadCanvas.Invalidate();
                    diskWriteCanvas.Invalidate();
                });
            };

            processorCanvas.Paint += (o, e) =>
            {
                e.Graphics.FillRectangle(purple, 8, 128 - cpu, 13, cpu );
                e.Graphics.FillRectangle(green, 29, 128 - cpuT, 13, cpuT);
            };

            memoryCanvas.Paint += (o, e) =>
            {
                e.Graphics.FillRectangle(purple, 8, 128 - mem, 13, mem);
                e.Graphics.FillRectangle(green, 29, 128 - memT, 13, memT);
            };

            diskReadCanvas.Paint += (o, e) =>
            {
                e.Graphics.FillRectangle(purple, 8, 128 - disk, 13, disk);
                e.Graphics.FillRectangle(green, 29, 128 - diskT, 13, diskT);
            };

            diskWriteCanvas.Paint += (o, e) =>
            {
                e.Graphics.FillRectangle(purple, 8, 128 - diskWrite, 13, diskWrite);
                e.Graphics.FillRectangle(green, 29, 128 - diskWriteT, 13, diskWriteT);
            };
            performanceTimer.Start();
        }

        void StartTablesMeasure()
        {
            tablesTimer = new Timer();
            tablesTimer.Interval = 5000;
            int tablesAmount = 0;
            (int reads, int writes) op = (0, 0);
            double dbSize = 0;
            bool first = false;
            bool running = true;
            Task.Run(() =>
            {
                tablesAmount = SystemTables.TablesAmount();
                op = SystemTables.TablesAction(DateTime.Now.Month + "/" + DateTime.Now.Day + "/" + DateTime.Now.Year);
                dbSize = SystemTables.DbSize() / 1000000.0;
                running = false;
                first = true;
            });
            tablesTimer.Tick += (o, e) =>
            {
                if(first)
                {
                    tablesTimer.Interval = 60000;
                }
                tablesAmountLabel.Text = "Amount:" + tablesAmount;
                readsTableLabel.Text = "Reads Today:" + op.reads;
                writesTableLabel.Text = "Writes Today:" + op.writes;
                sizeInDiskLabel.Text = "Size In Disk:" + dbSize.ToString("0.0000") + "MB";
                if(!running)
                {
                    running = true;
                    Task.Run(() =>
                    {
                        tablesAmount = SystemTables.TablesAmount();
                        op = SystemTables.TablesAction(DateTime.Now.Month + "/" + DateTime.Now.Day + "/" + DateTime.Now.Year);
                        
                        running = false;
                    });
                }
                
            };
            tablesTimer.Start();
        }

        private void MainMenu_FormClosing(object sender, FormClosingEventArgs e)
        {
            performanceTimer.Stop();
        }
    }
}
