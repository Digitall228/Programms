using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;


namespace GovReestrParser
{
    public partial class Form1 : Form
    {
        private Excel.Application excelapp;
        private Excel.Window excelWindow;
        private Excel.Workbook excelappworkbook;
        Excel.Range m_objRange;

        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            excelapp = new Excel.Application();
            excelapp.Visible = true;
            Task.Delay(2000).Wait();

            excelapp.SheetsInNewWorkbook = 1;
            excelapp.Workbooks.Add(Type.Missing);
            Worksheet workSheet = (Worksheet)excelapp.ActiveSheet;
            workSheet.Cells[1, 1] = "ID";
            workSheet.Cells[1, "B"] = "Name";
            workSheet.Cells[1, 3] = "Age";
            int rowExcel = 2;
            workSheet.Cells[rowExcel, "A"] = "55758";
            workSheet.Cells[rowExcel, "B"] = "Ivan";
            workSheet.Cells[rowExcel, "C"] = "5";

            //Запрашивать сохранение
            excelapp.DisplayAlerts = true;

            string pathToXmlFile;
            pathToXmlFile = Environment.CurrentDirectory + "\\" + "data.xls";
            workSheet.SaveAs(pathToXmlFile);

            excelapp.Quit();
        }
    }
}
