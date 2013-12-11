using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using FISCA.DSAUtil;
using SmartSchool.Common;
using FISCA.Presentation.Controls;
using SmartSchool;
using SHSchool.Data;
using K12.Data;
using System.IO;

namespace ClassPeriodDetail
{
    public partial class SelectTypeForm : BaseForm
    {
        Campus.Configuration.ConfigData cd;
        private string _ConfigPrint;
        private string name = "假別設定";
        private BackgroundWorker _BGWAbsenceAndPeriodList;

        private List<String> typeList = new List<string>();
        private List<String> absenceList = new List<string>();

        public SelectTypeForm(String name)
        {
            InitializeComponent();

            _ConfigPrint = name; //設定檔名稱

            _BGWAbsenceAndPeriodList = new BackgroundWorker();
            _BGWAbsenceAndPeriodList.DoWork += new DoWorkEventHandler(_BGWAbsenceAndPeriodList_DoWork);
            _BGWAbsenceAndPeriodList.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_BGWAbsenceAndPeriodList_RunWorkerCompleted);
            _BGWAbsenceAndPeriodList.RunWorkerAsync();
        }

        private void _BGWAbsenceAndPeriodList_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            #region 預設畫面載入
            System.Windows.Forms.DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.HeaderText = "假別分類";
            colName.MinimumWidth = 70;
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            colName.Width = 70;
            this.dataGridViewX1.Columns.Add(colName);

            //設定DataGridView的欄位屬性
            foreach (string absence in absenceList)
            {
                System.Windows.Forms.DataGridViewCheckBoxColumn newCol = new DataGridViewCheckBoxColumn();
                newCol.HeaderText = absence;
                newCol.Width = 55;
                newCol.ReadOnly = false;
                newCol.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
                newCol.Tag = absence;
                newCol.ValueType = typeof(bool);
                this.dataGridViewX1.Columns.Add(newCol);
            }

            //新增一個row
            foreach(String type in typeList)
            {
                DataGridViewRow addrow = new DataGridViewRow();
                addrow.CreateCells(dataGridViewX1, type);
                addrow.Tag = type;
                this.dataGridViewX1.Rows.Add(addrow);
            }
            
            #endregion

            #region 讀取上次設定
            cd = Campus.Configuration.Config.User[_ConfigPrint];
            string StringLine = cd[name];
            if (!string.IsNullOrEmpty(StringLine))
            {
                try
                {
                    XmlElement config = DSXmlHelper.LoadXml(StringLine);
                    foreach (XmlElement elem in config.SelectNodes("//Type"))
                    {
                        String text = elem.GetAttribute("Text");
                        String value = elem.GetAttribute("Value");
                        foreach (DataGridViewRow row in dataGridViewX1.Rows)
                        {
                            foreach (DataGridViewCell cell in row.Cells)
                            {
                                if (cell.OwningRow.Tag == null || cell.OwningColumn.Tag == null) continue;
                                if (cell.OwningRow.Tag.ToString() == text && cell.OwningColumn.Tag.ToString() == value)
                                    cell.Value = true;
                            }
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("讀取上次設定失敗");
                }
            }

            #endregion
        }

        private void _BGWAbsenceAndPeriodList_DoWork(object sender, DoWorkEventArgs e)
        {
            //取得節次分類
            foreach (SHPeriodMappingInfo info in SHPeriodMapping.SelectAll())
            {
                if (!typeList.Contains(info.Type))
                    typeList.Add(info.Type);
            }

            //取得所有假別種類
            List<AbsenceMappingInfo> Absencelist = K12.Data.AbsenceMapping.SelectAll();
            foreach (AbsenceMappingInfo var in Absencelist)
            {
                if (!absenceList.Contains(var.Name))
                    absenceList.Add(var.Name);
            }
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            XmlElement config = new XmlDocument().CreateElement("TypeList");
            foreach (DataGridViewRow row in dataGridViewX1.Rows)
            {
                foreach(DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null)
                    {
                        bool value = false;
                        if (bool.TryParse(cell.Value.ToString(), out value))
                        {
                            if(value)
                            {
                                XmlElement type = config.OwnerDocument.CreateElement("Type");
                                type.SetAttribute("Text", cell.OwningRow.Tag.ToString());
                                type.SetAttribute("Value", cell.OwningColumn.Tag.ToString());
                                config.AppendChild(type);
                            }
                        }
                    }
                }
            }

            config.OwnerDocument.AppendChild(config);
            cd[name] = config.OuterXml;
            cd.Save();
            MessageBox.Show("設定已儲存");
            this.Close();
        }

        private void checkBoxX1_CheckedChanged(object sender, EventArgs e)
        {
            foreach(DataGridViewRow row in dataGridViewX1.Rows)
            {
                foreach(DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Tag == null) continue;
                    if (checkBoxX1.Checked)
                        cell.Value = true;
                    else
                        cell.Value = false;
                }
            }
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
